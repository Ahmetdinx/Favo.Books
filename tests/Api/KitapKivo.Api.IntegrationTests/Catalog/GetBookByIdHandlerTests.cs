using KitapKivo.Modules.Catalog.Application.Books.GetById;
using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Domain.Categories;
using KitapKivo.Modules.Catalog.Domain.Editions;
using KitapKivo.Modules.Catalog.Domain.Publishers;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Api.IntegrationTests.Catalog;

public sealed class GetBookByIdHandlerTests
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly KitapKivoWebApplicationFactory _factory;

    public GetBookByIdHandlerTests(
        KitapKivoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HandleAsync_WhenBookExists_ShouldReturnEditionDetails()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        var book = new Book(
            "The Hobbit",
            "eng",
            description:
                "A fantasy novel by J.R.R. Tolkien.",
            firstPublishedYear: 1937);

        var publisher =
            new Publisher("HarperCollins");

        var edition = new Edition(
            book.Id,
            "The Hobbit",
            "eng",
            EditionFormat.Paperback,
            publisher.Id,
            isbn10: "0261102214",
            isbn13: "9780261102217",
            pageCount: 310,
            publicationYear: 2012,
            coverImageUrl:
                "https://example.com/the-hobbit.jpg");

        string categorySlug =
            $"fantasy-{Guid.NewGuid():N}";

        var category = new Category(
            "Fantasy",
            categorySlug);

        var bookCategory = new BookCategory(
            book.Id,
            category.Id);

        dbContext.Books.Add(book);
        dbContext.Publishers.Add(publisher);
        dbContext.Editions.Add(edition);
        dbContext.Categories.Add(category);
        dbContext.BookCategories.Add(bookCategory);

        await dbContext.SaveChangesAsync();

        GetBookByIdHandler handler =
            scope.ServiceProvider.GetRequiredService<
                GetBookByIdHandler>();

        // Act
        GetBookByIdResult? result =
            await handler.HandleAsync(
                new GetBookByIdQuery(book.Id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(book.Id, result.BookId);
        Assert.Equal("The Hobbit", result.Title);

        Assert.Equal(
            edition.Id,
            result.PrimaryEditionId);

        Assert.Equal(
            "https://example.com/the-hobbit.jpg",
            result.PrimaryCoverImageUrl);

        GetBookCategoryResult categoryResult =
            Assert.Single(result.Categories);

        Assert.Equal(
            category.Id,
            categoryResult.CategoryId);

        Assert.Equal(
            "Fantasy",
            categoryResult.Name);

        Assert.Equal(
            categorySlug,
            categoryResult.Slug);

        GetBookEditionResult editionResult =
            Assert.Single(result.Editions);

        Assert.Equal(
            edition.Id,
            editionResult.EditionId);

        Assert.Equal(
            "The Hobbit",
            editionResult.Title);

        Assert.Equal(
            "eng",
            editionResult.LanguageCode);

        Assert.Equal(
            "Paperback",
            editionResult.Format);

        Assert.Equal(
            "0261102214",
            editionResult.Isbn10);

        Assert.Equal(
            "9780261102217",
            editionResult.Isbn13);

        Assert.Equal(
            310,
            editionResult.PageCount);

        Assert.Equal(
            2012,
            editionResult.PublicationYear);

        Assert.Equal(
            "https://example.com/the-hobbit.jpg",
            editionResult.CoverImageUrl);

        Assert.NotNull(editionResult.Publisher);

        Assert.Equal(
            publisher.Id,
            editionResult.Publisher.PublisherId);

        Assert.Equal(
            "HarperCollins",
            editionResult.Publisher.Name);
    }

    [Fact]
    public async Task HandleAsync_WhenSeveralEditionsExist_ShouldSelectCoveredTurkishEditionAsPrimary()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        var book = new Book(
            "Primary edition selection test",
            "eng");

        var turkishEditionWithoutCover = new Edition(
            book.Id,
            "Kapaksız Türkçe Baskı",
            "tur",
            EditionFormat.Paperback,
            publicationYear: 2025);

        var englishEditionWithCover = new Edition(
            book.Id,
            "English Edition",
            "eng",
            EditionFormat.Hardcover,
            isbn13: "9780000000001",
            pageCount: 320,
            publicationYear: 2024,
            coverImageUrl:
                "https://example.com/english-cover.jpg");

        var turkishEditionWithCover = new Edition(
            book.Id,
            "Türkçe Baskı",
            "tur",
            EditionFormat.Paperback,
            publicationYear: 2020,
            coverImageUrl:
                "https://example.com/turkish-cover.jpg");

        dbContext.Books.Add(book);

        dbContext.Editions.AddRange(
            turkishEditionWithoutCover,
            englishEditionWithCover,
            turkishEditionWithCover);

        await dbContext.SaveChangesAsync();

        GetBookByIdHandler handler =
            scope.ServiceProvider.GetRequiredService<
                GetBookByIdHandler>();

        // Act
        GetBookByIdResult? result =
            await handler.HandleAsync(
                new GetBookByIdQuery(book.Id));

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            turkishEditionWithCover.Id,
            result.PrimaryEditionId);

        Assert.Equal(
            "https://example.com/turkish-cover.jpg",
            result.PrimaryCoverImageUrl);

        Assert.Equal(3, result.Editions.Count);
    }
}