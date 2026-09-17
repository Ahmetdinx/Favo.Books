using KitapKivo.Modules.Catalog.Application
    .Books.Browse;
using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Domain.Editions;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Api.IntegrationTests.Catalog;

public sealed class BrowseBooksHandlerTests
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly KitapKivoWebApplicationFactory
        _factory;

    public BrowseBooksHandlerTests(
        KitapKivoWebApplicationFactory factory)
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

        Assert.Empty(englishResult.Items);
    }
}
