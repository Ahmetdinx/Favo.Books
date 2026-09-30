using Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application.Books;
using Favo.Books.Modules.Catalog.Application.Books.Create;
using Favo.Books.Modules.Catalog.Application.Books.ImportExternal;
using Favo.Books.Modules.Catalog.Application.Books.ResolveExternal;
using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Sources;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class NovelImportPolicyTests(FavoBooksWebApplicationFactory factory)
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private static ApprovedNovel Approved => ApprovedNovelCatalog.Default.Find("/works/OL102749W")!;
    private static ExternalBookDetails Details => new("OpenLibrary", Approved.WorkId, Approved.WorkTitle,
        null, [new(null, "Herman Melville")], 1851, null, [], ["eng"], ["fiction"]);
    private static ExternalBookEditionDetails Edition => new("OpenLibrary", "/books/OL32200062M",
        Approved.TurkishTitle, "tur", "Paperback", null, "9789750741388", null, 2019,
        "https://covers.openlibrary.org/b/id/123-M.jpg", "Can");

    [Fact]
    public async Task UnreviewedWorkIsRejectedBeforeProviderCalls()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var source = new Source(Details, [Edition]);
        var result = await new ResolveExternalBookHandler(db, source, source)
            .HandleAsync(new("OpenLibrary", "/works/OL999999999W"));
        Assert.Equal("novel_review_required", result.RejectionReason);
        Assert.Equal(0, source.Calls);
    }

    [Theory]
    [InlineData("wrong-author", "reviewed_identity_mismatch")]
    [InlineData("wrong-title", "reviewed_identity_mismatch")]
    [InlineData("redirect", "novel_review_required")]
    [InlineData("english", "covered_turkish_edition_required")]
    [InlineData("blank-cover", "covered_turkish_edition_required")]
    [InlineData("wrong-edition-title", "covered_turkish_edition_required")]
    public async Task InvalidSourceDoesNotWriteAnyCatalogEntities(string scenario, string reason)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var details = scenario switch
        {
            "wrong-author" => Details with { Authors = [new(null, "Different author")] },
            "wrong-title" => Details with { Title = "A study of Moby Dick" },
            "redirect" => Details with { ExternalId = "/works/OL999999999W" },
            _ => Details
        };
        var edition = scenario switch
        {
            "english" => Edition with { LanguageCode = "eng" },
            "blank-cover" => Edition with { CoverUrl = " " },
            "wrong-edition-title" => Edition with { Title = "Another novel" },
            _ => Edition
        };
        var before = (await db.Books.CountAsync(), await db.Authors.CountAsync(), await db.Editions.CountAsync(), await db.NovelReviews.CountAsync());
        var source = new Source(details, [edition]);
        var result = await new ResolveExternalBookHandler(db, source, source).HandleAsync(new("OpenLibrary", Approved.WorkId));
        Assert.Equal(reason, result.RejectionReason);
        Assert.Equal(before, (await db.Books.CountAsync(), await db.Authors.CountAsync(), await db.Editions.CountAsync(), await db.NovelReviews.CountAsync()));
    }

    [Fact]
    public async Task ApprovedImportStoresReviewAndSelectsCoveredEdition_RevocationBlocksReuse()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var source = new Source(Details, [Edition with { CoverUrl = null }, Edition]);
        var handler = new ResolveExternalBookHandler(db, source, source);
        var command = new ResolveExternalBookCommand("OpenLibrary", Approved.WorkId);
        var result = await handler.HandleAsync(command);
        Assert.True(result.IsCreated);
        Assert.True(await db.Books.EligibleNovels().AnyAsync(b => b.Id == result.BookId));
        var review = await db.NovelReviews.SingleAsync(r => r.BookId == result.BookId);
        Assert.Equal(Approved.SourceUrl, review.SourceUrl);
        Assert.True((await handler.HandleAsync(command)).IsSuccess);
        db.NovelReviews.Add(new NovelReview(result.BookId!.Value, NovelDecision.NotNovel, "correction",
            "https://example.com/correction", "Fixture revocation", "test", "v1"));
        await db.SaveChangesAsync();
        var calls = source.Calls;
        Assert.Equal("existing_book_not_eligible", (await handler.HandleAsync(command)).RejectionReason);
        Assert.Equal(calls, source.Calls);
    }

    [Fact]
    public async Task LegacyExistingRecordAndFreeFormCreationCannotBypassReview()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var author = new Author("Legacy Author");
        var book = new Book("Legacy unreviewed", "tur");
        book.ExternalReferences.Add(ExternalReference.CreateForBook(book.Id, CatalogSource.OpenLibrary, Approved.WorkId));
        db.Books.Add(book);
        db.Authors.Add(author);
        await db.SaveChangesAsync();
        var source = new Source(Details, [Edition]);
        Assert.Equal("existing_book_not_eligible", (await new ResolveExternalBookHandler(db, source, source)
            .HandleAsync(new("OpenLibrary", Approved.WorkId))).RejectionReason);
        var count = await db.Books.CountAsync();
        var result = await new CreateBookHandler(db).HandleAsync(new("Unreviewed novel", "tur", null, null, null, [author.Id]));
        Assert.Equal("novel_review_required", result.RejectionReason);
        Assert.Equal(count, await db.Books.CountAsync());
    }

    [Fact]
    public async Task BatchReportsPolicyRejectionAsFailureRatherThanMissingBook()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var source = new Source(Details, [Edition]);
        var batch = new ImportExternalBooksHandler(source, new ResolveExternalBookHandler(db, source, source));
        var result = await batch.HandleAsync(new("OpenLibrary", "fiction", 1, 1));
        Assert.Equal(1, result.FailedCount);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.NotFoundCount);
        Assert.Equal(0, source.Calls);
    }

    private sealed class Source(ExternalBookDetails details, ExternalBookEditionDetails[] editions)
        : IExternalBookDetailsClient, IExternalBookEditionClient, IExternalBookSearchClient
    {
        public int Calls { get; private set; }
        public Task<ExternalBookDetails?> GetByIdAsync(string id, CancellationToken ct = default)
        { Calls++; return Task.FromResult<ExternalBookDetails?>(details); }
        public Task<ExternalBookEditionDetails?> GetBestForWorkAsync(string id, CancellationToken ct = default)
            => Task.FromResult<ExternalBookEditionDetails?>(editions.FirstOrDefault());
        public Task<IReadOnlyList<ExternalBookEditionDetails>> GetBestEditionsForWorkAsync(string id, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ExternalBookEditionDetails>>(editions);
        public Task<ExternalBookSearchResult> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult(new ExternalBookSearchResult(1, page, pageSize,
                [new("OpenLibrary", "/works/OL999999999W", "Unreviewed", [], null, null, [], [])]));
    }
}
