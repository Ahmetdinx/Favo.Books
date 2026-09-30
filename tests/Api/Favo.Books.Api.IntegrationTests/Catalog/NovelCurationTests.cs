using Favo.Books.Modules.Catalog.Application.Books;
using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Application.Books.Browse;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Domain.Sources;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class NovelCurationTests(FavoBooksWebApplicationFactory factory)
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    [Fact]
    public async Task CatalogAndRecommendationsHideUnknownAndRevokedBooksWithoutDeletingThem()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var (book, entry) = await Seed(db);
        var browse = scope.ServiceProvider.GetRequiredService<Favo.Books.Modules.Catalog.Application.Books.Browse.BrowseBooksHandler>();
        var discovery = scope.ServiceProvider.GetRequiredService<Favo.Books.SharedKernel.Catalog.IDiscoveryCatalog>();
        var query = new Favo.Books.Modules.Catalog.Application.Books.Browse.BrowseBooksQuery(
            book.Title, CatalogBookSearchField.Title, null, 1, 10, CatalogBookSort.Title);
        Assert.Empty((await browse.HandleAsync(query)).Items);
        Assert.DoesNotContain(await discovery.GetBooksAsync(), b => b.BookId == book.Id);
        await new ReviewNovelBatch(db).ExecuteAsync(Batch(entry), true);
        Assert.Single((await browse.HandleAsync(query)).Items);
        Assert.Contains(await discovery.GetBooksAsync(), b => b.BookId == book.Id);
        await new ReviewNovelBatch(db).ExecuteAsync(Batch(entry with { Decision = NovelDecision.Unknown }), true);
        Assert.Empty((await browse.HandleAsync(query)).Items);
        Assert.DoesNotContain(await discovery.GetBooksAsync(), b => b.BookId == book.Id);
        Assert.True(await db.Books.AnyAsync(b => b.Id == book.Id));
        Assert.True(await db.Editions.AnyAsync(e => e.BookId == book.Id));
    }

    [Fact]
    public async Task PreviewDoesNotWrite_ApplyIsIdempotent_CorrectionPreservesHistory()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var (book, entry) = await Seed(db);
        var handler = new ReviewNovelBatch(db);
        var batch = Batch(entry);
        Assert.False(await db.Books.EligibleNovels().AnyAsync(b => b.Id == book.Id));
        Assert.False((await handler.ExecuteAsync(batch, false)).Applied);
        Assert.False(await db.NovelReviews.AnyAsync(r => r.BookId == book.Id));
        Assert.Equal(1, (await handler.ExecuteAsync(batch, true)).Added);
        Assert.Equal(0, (await handler.ExecuteAsync(batch, true)).Added);
        Assert.True(await db.Books.EligibleNovels().AnyAsync(b => b.Id == book.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ExecuteAsync(
            batch with { Entries = [entry with { Decision = NovelDecision.NotNovel }] }, true));
        await handler.ExecuteAsync(Batch(entry with { Decision = NovelDecision.NotNovel }), true);
        Assert.False(await db.Books.EligibleNovels().AnyAsync(b => b.Id == book.Id));
        Assert.Equal(2, await db.NovelReviews.CountAsync(r => r.BookId == book.Id));
        Assert.True(await db.Books.AnyAsync(b => b.Id == book.Id));
    }

    [Fact]
    public async Task WrongAuthorInAnyEntryRejectsEntireBatch()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var (first, one) = await Seed(db);
        var (second, two) = await Seed(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewNovelBatch(db).ExecuteAsync(
            Batch(one, two with { ExpectedAuthor = "Different author with same book title" }), true));
        Assert.False(await db.NovelReviews.AnyAsync(r => r.BookId == first.Id || r.BookId == second.Id));
    }

    [Fact]
    public async Task EditionEligibilityIsCheckedAtReviewAndAtQueryTime()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var (book, entry) = await Seed(db);
        await new ReviewNovelBatch(db).ExecuteAsync(Batch(entry), true);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE catalog.\"Editions\" SET \"LanguageCode\" = 'eng' WHERE \"BookId\" = {book.Id}");
        Assert.False(await db.Books.EligibleNovels().AnyAsync(b => b.Id == book.Id));
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ReviewNovelBatch(db).ExecuteAsync(Batch(entry), true));
    }

    private static NovelReviewBatch Batch(params NovelReviewEntry[] entries)
        => new(Guid.NewGuid().ToString("N"), "test-reviewer", "test-v1", entries);

    private static async Task<(Book, NovelReviewEntry)> Seed(CatalogDbContext db)
    {
        var book = new Book("Historical novel " + Guid.NewGuid(), "tur");
        var author = new Author("Test Author " + Guid.NewGuid());
        db.Authors.Add(author);
        book.Authors.Add(new BookAuthor(book.Id, author.Id, 0));
        book.Editions.Add(new Edition(book.Id, book.Title, "tur", EditionFormat.Paperback,
            coverImageUrl: "https://example.com/turkish-cover.jpg"));
        var workId = "/works/test-" + Guid.NewGuid();
        book.ExternalReferences.Add(ExternalReference.CreateForBook(book.Id, CatalogSource.OpenLibrary, workId));
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return (book, new(workId, book.Title, author.Name, NovelDecision.Novel,
            "https://publisher.example/novel", "Explicit novel classification, not a history textbook."));
    }
}
