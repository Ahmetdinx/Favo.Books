using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application
    .Books.ImportExternal;
using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Domain.Sources;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KitapKivo.Api.IntegrationTests.Catalog;

public sealed class ImportExternalBooksHandlerTests
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly KitapKivoWebApplicationFactory _factory;

    public ImportExternalBooksHandlerTests(
        KitapKivoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateNewBooksAndReuseExistingBook()
    {
        // Arrange
        ExternalBookSearchResult searchResult = new(
            3,
            1,
            3,
            [
                CreateSearchItem(
                    "/works/OL10001W",
                    "Existing Book"),
                CreateSearchItem(
                    "/works/OL10002W",
                    "First New Book"),
                CreateSearchItem(
                    "/works/OL10003W",
                    "Second New Book")
            ]);

        var searchClient =
            new StubExternalBookSearchClient(
                searchResult);

        var detailsClient =
            new StubExternalBookDetailsClient(
                new Dictionary<
                    string,
                    ExternalBookDetails>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["/works/OL10002W"] =
                        CreateDetails(
                            "/works/OL10002W",
                            "First New Book"),
                    ["/works/OL10003W"] =
                        CreateDetails(
                            "/works/OL10003W",
                            "Second New Book")
                });

        using WebApplicationFactory<Program> application =
            CreateApplication(
                searchClient,
                detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        var existingBook = new Book(
            "Existing Book",
            "eng");

        existingBook.ExternalReferences.Add(
            ExternalReference.CreateForBook(
                existingBook.Id,
                CatalogSource.OpenLibrary,
                "/works/OL10001W"));

        dbContext.Books.Add(existingBook);

        await dbContext.SaveChangesAsync();

        ImportExternalBooksHandler handler =
            scope.ServiceProvider.GetRequiredService<
                ImportExternalBooksHandler>();

        // Act
        ImportExternalBooksResult result =
            await handler.HandleAsync(
                new ImportExternalBooksCommand(
                    "OpenLibrary",
                    "fantasy",
                    1,
                    3));

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(3, result.FoundCount);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(1, result.ExistingCount);
        Assert.Equal(0, result.NotFoundCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Failures);

        Assert.Equal(1, searchClient.CallCount);
        Assert.Equal("fantasy", searchClient.Query);
        Assert.Equal(1, searchClient.Page);
        Assert.Equal(3, searchClient.PageSize);

        Assert.Equal(2, detailsClient.CallCount);

        int bookReferenceCount =
            await dbContext.ExternalReferences
                .AsNoTracking()
                .CountAsync(reference =>
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.BookId.HasValue);

        Assert.Equal(3, bookReferenceCount);
    }

    private WebApplicationFactory<Program>
        CreateApplication(
            IExternalBookSearchClient searchClient,
            IExternalBookDetailsClient detailsClient)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IExternalBookSearchClient>();

                services.RemoveAll<
                    IExternalBookDetailsClient>();

                services.AddSingleton(searchClient);
                services.AddSingleton(detailsClient);
            });
        });
    }

    private static ExternalBookSearchItem
        CreateSearchItem(
            string externalId,
            string title)
    {
        return new ExternalBookSearchItem(
            "OpenLibrary",
            externalId,
            title,
            [],
            null,
            null,
            [],
            ["eng"]);
    }

    private static ExternalBookDetails CreateDetails(
        string externalId,
        string title)
    {
        return new ExternalBookDetails(
            "OpenLibrary",
            externalId,
            title,
            null,
            [],
            null,
            null,
            [],
            ["eng"],
            []);
    }

    private sealed class StubExternalBookSearchClient(
        ExternalBookSearchResult result)
        : IExternalBookSearchClient
    {
        public int CallCount { get; private set; }

        public string? Query { get; private set; }

        public int Page { get; private set; }

        public int PageSize { get; private set; }

        public Task<ExternalBookSearchResult> SearchAsync(
            string query,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Query = query;
            Page = page;
            PageSize = pageSize;

            return Task.FromResult(result);
        }
    }

    private sealed class StubExternalBookDetailsClient(
        IReadOnlyDictionary<
            string,
            ExternalBookDetails> results)
        : IExternalBookDetailsClient
    {
        public int CallCount { get; private set; }

        public Task<ExternalBookDetails?> GetByIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            results.TryGetValue(
                externalId,
                out ExternalBookDetails? result);

            return Task.FromResult(result);
        }
    }
}