using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Categories;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class CatalogBookSummariesEndpointTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private readonly FavoBooksWebApplicationFactory
        _factory;

    public CatalogBookSummariesEndpointTests(
        FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        GetBookSummaries_WithValidRequest_ReturnsRequestedBooksWithPreferredMetadata()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        CatalogDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<CatalogDbContext>();

        string uniqueValue =
            Guid.NewGuid().ToString("N");

        var turkishPreferredBook = new Book(
            $"Original Turkish Summary {uniqueValue}",
            "eng",
            firstPublishedYear: 1937);

        var englishFallbackBook = new Book(
            $"Original English Summary {uniqueValue}",
            "fre",
            firstPublishedYear: 1965);

        var turkishAuthor = new Author(
            $"Turkish Summary Author {uniqueValue}");

        var englishAuthor = new Author(
            $"English Summary Author {uniqueValue}");

        var category = new Category(
            $"Summary Category {uniqueValue}",
            $"summary-{uniqueValue}");

        var turkishEdition = new Edition(
            turkishPreferredBook.Id,
            "Türkçe Profil Başlığı",
            "tur",
            EditionFormat.Paperback,
            publicationYear: 2020,
            coverImageUrl:
                "https://example.com/" +
                "turkish-summary-cover.jpg");

        var englishEdition = new Edition(
            turkishPreferredBook.Id,
            "English Profile Title",
            "eng",
            EditionFormat.Hardcover,
            publicationYear: 2024,
            coverImageUrl:
                "https://example.com/" +
                "english-summary-cover.jpg");

        var fallbackEnglishEdition = new Edition(
            englishFallbackBook.Id,
            "English Fallback Summary",
            "eng",
            EditionFormat.Paperback,
            publicationYear: 2018,
            coverImageUrl:
                "https://example.com/" +
                "english-fallback-summary.jpg");

        var fallbackFrenchEdition = new Edition(
            englishFallbackBook.Id,
            "Résumé Français",
            "fre",
            EditionFormat.Hardcover,
            publicationYear: 2025,
            coverImageUrl:
                "https://example.com/" +
                "french-summary-cover.jpg");

        var turkishBookAuthor = new BookAuthor(
            turkishPreferredBook.Id,
            turkishAuthor.Id,
            displayOrder: 0);

        var englishBookAuthor = new BookAuthor(
            englishFallbackBook.Id,
            englishAuthor.Id,
            displayOrder: 0);

        var bookCategory = new BookCategory(
            turkishPreferredBook.Id,
            category.Id);

        dbContext.Books.AddRange(
            turkishPreferredBook,
            englishFallbackBook);

        dbContext.Authors.AddRange(
            turkishAuthor,
            englishAuthor);

        dbContext.Categories.Add(category);

        dbContext.Editions.AddRange(
            turkishEdition,
            englishEdition,
            fallbackEnglishEdition,
            fallbackFrenchEdition);

        dbContext.BookAuthors.AddRange(
            turkishBookAuthor,
            englishBookAuthor);

        dbContext.BookCategories.Add(
            bookCategory);

        await dbContext.SaveChangesAsync();

        using HttpClient client =
            _factory.CreateClient();

        Guid missingBookId = Guid.NewGuid();

        var request = new
        {
            BookIds = new[]
            {
                turkishPreferredBook.Id,
                englishFallbackBook.Id,
                turkishPreferredBook.Id,
                missingBookId
            }
        };

        // Act
        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/catalog/books/summaries",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kitap özeti yanıtının " +
                "gövdesi boş döndü.");

        JsonElement[] items = responseBody
            .RootElement
            .GetProperty("items")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(2, items.Length);

        JsonElement turkishItem =
            Assert.Single(
                items,
                item =>
                    item.GetProperty("bookId")
                        .GetGuid() ==
                    turkishPreferredBook.Id);

        Assert.Equal(
            "Türkçe Profil Başlığı",
            turkishItem.GetProperty("title")
                .GetString());

        Assert.Equal(
            1937,
            turkishItem
                .GetProperty("firstPublishedYear")
                .GetInt32());

        Assert.Equal(
            "https://example.com/" +
            "turkish-summary-cover.jpg",
            turkishItem
                .GetProperty("coverImageUrl")
                .GetString());

        JsonElement turkishAuthorItem =
            Assert.Single(
                turkishItem
                    .GetProperty("authors")
                    .EnumerateArray());

        Assert.Equal(
            turkishAuthor.Id,
            turkishAuthorItem
                .GetProperty("authorId")
                .GetGuid());

        Assert.Equal(
            turkishAuthor.Name,
            turkishAuthorItem
                .GetProperty("name")
                .GetString());

        JsonElement categoryItem =
            Assert.Single(
                turkishItem
                    .GetProperty("categories")
                    .EnumerateArray());

        Assert.Equal(
            category.Id,
            categoryItem
                .GetProperty("categoryId")
                .GetGuid());

        Assert.Equal(
            category.Slug,
            categoryItem
                .GetProperty("slug")
                .GetString());

        JsonElement englishItem =
            Assert.Single(
                items,
                item =>
                    item.GetProperty("bookId")
                        .GetGuid() ==
                    englishFallbackBook.Id);

        Assert.Equal(
            "English Fallback Summary",
            englishItem.GetProperty("title")
                .GetString());

        Assert.Null(englishItem
                .GetProperty("coverImageUrl")
                .GetString());

        Assert.DoesNotContain(
            items,
            item =>
                item.GetProperty("bookId")
                    .GetGuid() ==
                missingBookId);
    }

    [Fact]
    public async Task
        GetBookSummaries_WithoutBookIds_ReturnsBadRequest()
    {
        // Arrange
        using HttpClient client =
            _factory.CreateClient();

        var request = new
        {
            BookIds = Array.Empty<Guid>()
        };

        // Act
        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/catalog/books/summaries",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Doğrulama yanıtının " +
                "gövdesi boş döndü.");

        JsonElement errors =
            responseBody.RootElement
                .GetProperty("errors");

        Assert.Equal(
            "En az bir kitap kimliği belirtilmelidir.",
            errors.GetProperty("bookIds")[0]
                .GetString());
    }

    [Fact]
    public async Task
        GetBookSummaries_WithTooManyAndEmptyBookIds_ReturnsBadRequest()
    {
        // Arrange
        using HttpClient client =
            _factory.CreateClient();

        Guid[] bookIds = Enumerable
            .Range(0, 100)
            .Select(_ => Guid.NewGuid())
            .Append(Guid.Empty)
            .ToArray();

        var request = new
        {
            BookIds = bookIds
        };

        // Act
        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/catalog/books/summaries",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Doğrulama yanıtının " +
                "gövdesi boş döndü.");

        string?[] errorMessages =
            responseBody.RootElement
                .GetProperty("errors")
                .GetProperty("bookIds")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray();

        Assert.Contains(
            "Bir istekte en fazla 100 kitap " +
            "kimliği gönderilebilir.",
            errorMessages);

        Assert.Contains(
            "Kitap kimlikleri boş olamaz.",
            errorMessages);
    }
}
