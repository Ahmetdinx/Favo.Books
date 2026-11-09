using Favo.Books.Modules.Catalog.Application
    .Books.Browse;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class BrowseBooksHandlerTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private readonly FavoBooksWebApplicationFactory
        _factory;

    public BrowseBooksHandlerTests(
        FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        HandleAsync_ShouldPreferTurkishEdition_AndHideEnglishFallbackCover()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        string uniqueValue =
            Guid.NewGuid().ToString("N");

        var turkishPreferredBook = new Book(
            $"Original Turkish Test {uniqueValue}",
            "eng");

        var turkishEdition = new Edition(
            turkishPreferredBook.Id,
            "Türkçe Tercih Edilen Başlık",
            "tur",
            EditionFormat.Paperback,
            publicationYear: 2020,
            coverImageUrl:
                "https://example.com/turkish-cover.jpg");

        var englishEdition = new Edition(
            turkishPreferredBook.Id,
            "English Title",
            "eng",
            EditionFormat.Hardcover,
            publicationYear: 2024,
            coverImageUrl:
                "https://example.com/english-cover.jpg");

        var frenchEdition = new Edition(
            turkishPreferredBook.Id,
            "Titre Français",
            "fre",
            EditionFormat.Paperback,
            publicationYear: 2025,
            coverImageUrl:
                "https://example.com/french-cover.jpg");

        var englishFallbackBook = new Book(
            $"Original English Test {uniqueValue}",
            "fre");

        var fallbackEnglishEdition = new Edition(
            englishFallbackBook.Id,
            "English Fallback Title",
            "eng",
            EditionFormat.Paperback,
            publicationYear: 2018,
            coverImageUrl:
                "https://example.com/english-fallback.jpg");

        var fallbackFrenchEdition = new Edition(
            englishFallbackBook.Id,
            "Titre Français Récent",
            "fre",
            EditionFormat.Hardcover,
            publicationYear: 2025,
            coverImageUrl:
                "https://example.com/french-fallback.jpg");

        dbContext.Books.AddRange(
            turkishPreferredBook,
            englishFallbackBook);

        dbContext.Editions.AddRange(
            turkishEdition,
            englishEdition,
            frenchEdition,
            fallbackEnglishEdition,
            fallbackFrenchEdition);

        await dbContext.SaveChangesAsync();

        dbContext.NovelReviews.Add(new NovelReview(turkishPreferredBook.Id, NovelDecision.Novel,
            uniqueValue, "https://example.com/novel", "Verified fixture", "test", "test-v1"));
        await dbContext.SaveChangesAsync();

        BrowseBooksHandler handler =
            scope.ServiceProvider.GetRequiredService<
                BrowseBooksHandler>();

        // Act
        BrowseBooksResult turkishResult =
            await handler.HandleAsync(
                new BrowseBooksQuery(
                    turkishPreferredBook.Title,
                    CatalogBookSearchField.Title,
                    null,
                    1,
                    10,
                    CatalogBookSort.Title));

        BrowseBooksResult englishResult =
            await handler.HandleAsync(
                new BrowseBooksQuery(
                    englishFallbackBook.Title,
                    CatalogBookSearchField.Title,
                    null,
                    1,
                    10,
                    CatalogBookSort.Title));

        // Assert
        BrowseBookItemResult turkishItem =
            Assert.Single(
                turkishResult.Items);

        Assert.Equal(
            turkishPreferredBook.Id,
            turkishItem.BookId);

        Assert.Equal(
            "Türkçe Tercih Edilen Başlık",
            turkishItem.Title);

        Assert.Equal(
            "https://example.com/turkish-cover.jpg",
            turkishItem.CoverImageUrl);

        Assert.DoesNotContain(
            englishResult.Items,
            item => item.BookId == englishFallbackBook.Id);
    }

    [Theory]
    [InlineData("kurk mantolu")]
    [InlineData("KÜRK MANTOLU")]
    [InlineData("madonna kürk")]
    [InlineData("sabahattin kurk")]
    [InlineData("ALİ madonna")]
    public async Task Search_MatchesTurkishLettersAndWordsAcrossFields(string text)
    {
        using var scope = _factory.Services.CreateScope();
        var marker = Guid.NewGuid().ToString("N");
        var book = await SeedBook(scope, $"Kürk Mantolu Madonna {marker}", "Sabahattin Ali");
        var handler = scope.ServiceProvider.GetRequiredService<BrowseBooksHandler>();

        var result = await handler.HandleAsync(
            new BrowseBooksQuery($"{text} {marker}", CatalogBookSearchField.All, null, 1, 10, CatalogBookSort.RecentlyAdded));

        Assert.Equal(book.Id, Assert.Single(result.Items).BookId);
    }

    [Fact]
    public async Task Search_FindsBookByHyphenatedIsbn()
    {
        using var scope = _factory.Services.CreateScope();
        var digits = Random.Shared.NextInt64(1_000_000_000, 9_999_999_999).ToString();
        var book = await SeedBook(scope, $"Isbn {Guid.NewGuid():N}", "Yazar", "978" + digits);
        var handler = scope.ServiceProvider.GetRequiredService<BrowseBooksHandler>();

        var result = await handler.HandleAsync(
            new BrowseBooksQuery($"978-{digits}", CatalogBookSearchField.All, null, 1, 10, CatalogBookSort.RecentlyAdded));

        Assert.Equal(book.Id, Assert.Single(result.Items).BookId);
    }

    [Fact]
    public async Task Search_TreatsWildcardsAsLiteralText()
    {
        using var scope = _factory.Services.CreateScope();
        var marker = Guid.NewGuid().ToString("N");
        await SeedBook(scope, $"Joker {marker}", "Yazar");
        var handler = scope.ServiceProvider.GetRequiredService<BrowseBooksHandler>();

        var result = await handler.HandleAsync(
            new BrowseBooksQuery($"J_ker% {marker}", CatalogBookSearchField.All, null, 1, 10, CatalogBookSort.RecentlyAdded));

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Search_RanksTitlePrefixFirstUnderDefaultSort()
    {
        using var scope = _factory.Services.CreateScope();
        var marker = Guid.NewGuid().ToString("N");
        var prefix = await SeedBook(scope, $"{marker} Başlangıç", "Yazar");
        await SeedBook(scope, $"Sonra {marker}", "Yazar");
        var handler = scope.ServiceProvider.GetRequiredService<BrowseBooksHandler>();

        var result = await handler.HandleAsync(
            new BrowseBooksQuery(marker, CatalogBookSearchField.All, null, 1, 10, CatalogBookSort.RecentlyAdded));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(prefix.Id, result.Items[0].BookId);
    }

    private static async Task<Book> SeedBook(IServiceScope scope, string title, string authorName, string? isbn13 = null)
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var book = new Book(title, "tur");
        var author = new Favo.Books.Modules.Catalog.Domain.Authors.Author(authorName);
        db.Books.Add(book);
        db.Authors.Add(author);
        db.BookAuthors.Add(new BookAuthor(book.Id, author.Id, 1));
        db.Editions.Add(new Edition(book.Id, title, "tur", EditionFormat.Paperback,
            isbn13: isbn13, coverImageUrl: "https://example.com/test.jpg"));
        db.NovelReviews.Add(new NovelReview(book.Id, NovelDecision.Novel, Guid.NewGuid().ToString("N"),
            "https://example.com/novel", "Verified fixture", "test", "test-v1"));
        await db.SaveChangesAsync();
        return book;
    }

    [Theory]
    [InlineData(CatalogBookSort.Title)]
    [InlineData(CatalogBookSort.RecentlyAdded)]
    [InlineData(CatalogBookSort.FirstPublishedNewest)]
    public async Task PersonalOrderingDemotesBeforePagination(CatalogBookSort sort)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var marker = Guid.NewGuid().ToString("N");
        var books = Enumerable.Range(0, 3).Select(i => new Book($"{marker} {i}", "tur")).ToArray();
        db.Books.AddRange(books);
        foreach (var book in books)
        {
            db.Editions.Add(new Edition(book.Id, book.Title, "tur", EditionFormat.Paperback,
                coverImageUrl: "https://example.com/test.jpg"));
            db.NovelReviews.Add(new NovelReview(book.Id, NovelDecision.Novel, marker,
                "https://example.com/novel", "Verified fixture", "test", "test-v1"));
        }
        await db.SaveChangesAsync();
        var handler = scope.ServiceProvider.GetRequiredService<BrowseBooksHandler>();
        var query = new BrowseBooksQuery(marker, CatalogBookSearchField.Title, null, 1, 1, sort);
        var original = await handler.HandleAsync(query);
        var demoted = original.Items.Single().BookId;
        var first = await handler.HandleAsync(query, demotedBookIds: [demoted]);
        var last = await handler.HandleAsync(query with { Page = 3 }, demotedBookIds: [demoted]);
        Assert.Equal(3, first.TotalCount);
        Assert.NotEqual(demoted, first.Items.Single().BookId);
        Assert.Equal(demoted, last.Items.Single().BookId);
    }
}
