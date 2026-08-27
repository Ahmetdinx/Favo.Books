using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;
using KitapKivo.Modules.Catalog.Domain.Authors;
using KitapKivo.Modules.Catalog.Domain.Categories;
using KitapKivo.Modules.Catalog.Domain.Editions;
using KitapKivo.Modules.Catalog.Domain.Sources;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KitapKivo.Api.IntegrationTests.Catalog;

public sealed class ResolveExternalBookHandlerTests
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly KitapKivoWebApplicationFactory
        _factory;

    public ResolveExternalBookHandlerTests(
        KitapKivoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task
        HandleAsync_WhenBookDoesNotExist_ShouldCreateBook()
    {
        // Arrange
        var details = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL10001W",
            "Dune",
            "Epic science fiction novel.",
            [
                new ExternalBookAuthor(
                    "/authors/OL20001A",
                    "Frank Herbert")
            ],
            1965,
            "https://example.com/dune.jpg",
            ["9780441013593"],
            ["eng"],
            ["Science Fiction"]);

        var detailsClient =
            new StubExternalBookDetailsClient(
                details);

        var editionDetails =
            new ExternalBookEditionDetails(
                "OpenLibrary",
                "/books/OL30001M",
                "Dune",
                "tur",
                "Paperback",
                "0441013597",
                "9780441013593",
                412,
                1965,
                "https://example.com/dune-edition.jpg",
                "Ace");

        var editionClient =
            new StubExternalBookEditionClient(
                editionDetails);

        using WebApplicationFactory<Program> application =
            CreateApplication(
                detailsClient,
                editionClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        ResolveExternalBookHandler handler =
            scope.ServiceProvider.GetRequiredService<
                ResolveExternalBookHandler>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        // Act
        ResolveExternalBookResult result =
            await handler.HandleAsync(
                new ResolveExternalBookCommand(
                    " openlibrary ",
                    " works/ol10001w "),
                cancellationTokenSource.Token);

        // Assert
        Assert.True(
            result.IsSuccess);

        Assert.True(
            result.IsCreated);

        Assert.NotNull(
            result.BookId);

        Assert.True(
            await dbContext.Books
                .AsNoTracking()
                .AnyAsync(book =>
                    book.Id ==
                    result.BookId.Value));

        Assert.True(
            await dbContext.ExternalReferences
                .AsNoTracking()
                .AnyAsync(reference =>
                    reference.BookId ==
                        result.BookId.Value &&
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/works/OL10001W"));

        Guid authorId =
            await dbContext.Books
                .AsNoTracking()
                .Where(book =>
                    book.Id ==
                    result.BookId.Value)
                .SelectMany(book =>
                    book.Authors)
                .Select(bookAuthor =>
                    bookAuthor.AuthorId)
                .SingleAsync();

        Assert.True(
            await dbContext.Authors
                .AsNoTracking()
                .AnyAsync(author =>
                    author.Id == authorId &&
                    author.Name ==
                        "Frank Herbert"));

        Assert.True(
            await dbContext.ExternalReferences
                .AsNoTracking()
                .AnyAsync(reference =>
                    reference.AuthorId ==
                        authorId &&
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/authors/OL20001A"));

        string[] categorySlugs =
            await dbContext.Books
                .AsNoTracking()
                .Where(book =>
                    book.Id ==
                    result.BookId.Value)
                .SelectMany(book =>
                    book.Categories)
                .Select(bookCategory =>
                    bookCategory.Category.Slug)
                .OrderBy(slug => slug)
                .ToArrayAsync();

        Assert.Equal(
            [
                CatalogCategorySlugs.ScienceFiction,
                CatalogCategorySlugs.Novel
            ],
            categorySlugs);

        Edition edition =
            await dbContext.Editions
                .AsNoTracking()
                .SingleAsync(item =>
                    item.BookId ==
                    result.BookId.Value);

        Assert.Equal(
            "Dune",
            edition.Title);

        Assert.Equal(
            "tur",
            edition.LanguageCode);

        Assert.Equal(
            EditionFormat.Paperback,
            edition.Format);

        Assert.Equal(
            "0441013597",
            edition.Isbn10);

        Assert.Equal(
            "9780441013593",
            edition.Isbn13);

        Assert.Equal(
            412,
            edition.PageCount);

        Assert.Equal(
            1965,
            edition.PublicationYear);

        Assert.Equal(
            "https://example.com/dune-edition.jpg",
            edition.CoverImageUrl);

        Assert.NotNull(
            edition.PublisherId);

        Assert.True(
            await dbContext.Publishers
                .AsNoTracking()
                .AnyAsync(publisher =>
                    publisher.Id ==
                        edition.PublisherId.Value &&
                    publisher.Name == "Ace"));

        Assert.True(
            await dbContext.ExternalReferences
                .AsNoTracking()
                .AnyAsync(reference =>
                    reference.EditionId ==
                        edition.Id &&
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/books/OL30001M"));

        Assert.Equal(
            1,
            detailsClient.CallCount);

        Assert.Equal(
            "/works/OL10001W",
            detailsClient.ExternalId);

        Assert.Equal(
            cancellationTokenSource.Token,
            detailsClient.CancellationToken);

        Assert.Equal(
            1,
            editionClient.CallCount);

        Assert.Equal(
            "/works/OL10001W",
            editionClient.ExternalWorkId);

        Assert.Equal(
            cancellationTokenSource.Token,
            editionClient.CancellationToken);
    }

    [Fact]
    public async Task
    HandleAsync_WhenTurkishAndEnglishEditionsExist_ShouldCreateOnlyTurkishEdition()
    {
        // Arrange
        var details = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL11005W",
            "Dune",
            "Epic science fiction novel.",
            [
                new ExternalBookAuthor(
                "/authors/OL21005A",
                "Frank Herbert")
            ],
            1965,
            null,
            ["9780441172719"],
            ["eng"],
            ["Science Fiction"]);

        var detailsClient =
            new StubExternalBookDetailsClient(
                details);

        ExternalBookEditionDetails[]
            editionDetails =
            [
                new ExternalBookEditionDetails(
                "OpenLibrary",
                "/books/OL31005M",
                "Dune: Çöl Gezegeni",
                "tur",
                "Paperback",
                null,
                "9789752112345",
                712,
                2023,
                "https://example.com/dune-turkish.jpg",
                "İthaki Yayınları"),

            new ExternalBookEditionDetails(
                "OpenLibrary",
                "/books/OL31006M",
                "Dune",
                "eng",
                "Hardcover",
                "0441172717",
                "9780441172719",
                896,
                1999,
                "https://example.com/dune-english.jpg",
                "Ace")
            ];

        var editionClient =
            new StubMultipleExternalBookEditionClient(
                editionDetails);

        using WebApplicationFactory<Program> application =
            CreateApplication(
                detailsClient,
                editionClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        ResolveExternalBookHandler handler =
            scope.ServiceProvider.GetRequiredService<
                ResolveExternalBookHandler>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        // Act
        ResolveExternalBookResult result =
            await handler.HandleAsync(
                new ResolveExternalBookCommand(
                    "OpenLibrary",
                    "/works/OL11005W"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsCreated);
        Assert.NotNull(result.BookId);

        Edition[] editions =
            await dbContext.Editions
                .AsNoTracking()
                .Where(edition =>
                    edition.BookId ==
                    result.BookId.Value)
                .OrderBy(edition =>
                    edition.LanguageCode)
                .ToArrayAsync();

        Assert.Single(editions);

        Edition turkishEdition =
            Assert.Single(
                editions,
                edition =>
                    edition.LanguageCode == "tur");

        Assert.Equal(
            "Dune: Çöl Gezegeni",
            turkishEdition.Title);

        Assert.Equal(
            "9789752112345",
            turkishEdition.Isbn13);

        Assert.Equal(
            "https://example.com/dune-turkish.jpg",
            turkishEdition.CoverImageUrl);

        Guid[] editionIds = editions
            .Select(edition => edition.Id)
            .ToArray();

        int editionReferenceCount =
            await dbContext.ExternalReferences
                .AsNoTracking()
                .CountAsync(reference =>
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.EditionId.HasValue &&
                    editionIds.Contains(
                        reference.EditionId.Value));

        Assert.Equal(
            1,
            editionReferenceCount);

        Assert.Equal(
            1,
            editionClient.CallCount);

        Assert.Equal(
            "/works/OL11005W",
            editionClient.ExternalWorkId);
    }

    [Fact]
    public async Task
        HandleAsync_WhenBookAlreadyExists_ShouldReturnExistingBook()
    {
        // Arrange
        var details = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL10002W",
            "Existing Book",
            null,
            [],
            2000,
            null,
            [],
            ["eng"],
            []);

        var detailsClient =
            new StubExternalBookDetailsClient(
                details);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        ResolveExternalBookHandler handler =
            scope.ServiceProvider.GetRequiredService<
                ResolveExternalBookHandler>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        var command =
            new ResolveExternalBookCommand(
                "OpenLibrary",
                "/works/OL10002W");

        // Act
        ResolveExternalBookResult firstResult =
            await handler.HandleAsync(
                command);

        ResolveExternalBookResult secondResult =
            await handler.HandleAsync(
                command);

        // Assert
        Assert.True(
            firstResult.IsSuccess);

        Assert.True(
            firstResult.IsCreated);

        Assert.True(
            secondResult.IsSuccess);

        Assert.False(
            secondResult.IsCreated);

        Assert.Equal(
            firstResult.BookId,
            secondResult.BookId);

        Assert.Equal(
            1,
            detailsClient.CallCount);

        int referenceCount =
            await dbContext.ExternalReferences
                .AsNoTracking()
                .CountAsync(reference =>
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/works/OL10002W" &&
                    reference.BookId.HasValue);

        Assert.Equal(
            1,
            referenceCount);
    }

    [Fact]
    public async Task
        HandleAsync_WhenExternalBookDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(
                null);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        ResolveExternalBookHandler handler =
            scope.ServiceProvider.GetRequiredService<
                ResolveExternalBookHandler>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        int bookCountBefore =
            await dbContext.Books.CountAsync();

        int authorCountBefore =
            await dbContext.Authors.CountAsync();

        // Act
        ResolveExternalBookResult result =
            await handler.HandleAsync(
                new ResolveExternalBookCommand(
                    "OpenLibrary",
                    "/works/OL404404W"));

        // Assert
        Assert.False(
            result.IsSuccess);

        Assert.False(
            result.IsCreated);

        Assert.Null(
            result.BookId);

        Assert.Equal(
            bookCountBefore,
            await dbContext.Books.CountAsync());

        Assert.Equal(
            authorCountBefore,
            await dbContext.Authors.CountAsync());

        Assert.False(
            await dbContext.ExternalReferences
                .AsNoTracking()
                .AnyAsync(reference =>
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/works/OL404404W"));

        Assert.Equal(
            1,
            detailsClient.CallCount);
    }

    [Fact]
    public async Task
        HandleAsync_WhenAuthorAlreadyExists_ShouldReuseAuthor()
    {
        // Arrange
        var details = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL10004W",
            "Author Reuse Test",
            null,
            [
                new ExternalBookAuthor(
                    "/authors/OL20004A",
                    "Existing Author")
            ],
            2020,
            null,
            [],
            ["eng"],
            []);

        var detailsClient =
            new StubExternalBookDetailsClient(
                details);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        var existingAuthor =
            new Author(
                "Existing Author");

        existingAuthor.ExternalReferences.Add(
            ExternalReference.CreateForAuthor(
                existingAuthor.Id,
                CatalogSource.OpenLibrary,
                "/authors/OL20004A"));

        dbContext.Authors.Add(
            existingAuthor);

        await dbContext.SaveChangesAsync();

        ResolveExternalBookHandler handler =
            scope.ServiceProvider.GetRequiredService<
                ResolveExternalBookHandler>();

        // Act
        ResolveExternalBookResult result =
            await handler.HandleAsync(
                new ResolveExternalBookCommand(
                    "OpenLibrary",
                    "/works/OL10004W"));

        // Assert
        Assert.True(
            result.IsSuccess);

        Assert.True(
            result.IsCreated);

        Assert.NotNull(
            result.BookId);

        Guid linkedAuthorId =
            await dbContext.Books
                .AsNoTracking()
                .Where(book =>
                    book.Id ==
                    result.BookId.Value)
                .SelectMany(book =>
                    book.Authors)
                .Select(bookAuthor =>
                    bookAuthor.AuthorId)
                .SingleAsync();

        Assert.Equal(
            existingAuthor.Id,
            linkedAuthorId);

        int authorReferenceCount =
            await dbContext.ExternalReferences
                .AsNoTracking()
                .CountAsync(reference =>
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/authors/OL20004A" &&
                    reference.AuthorId.HasValue);

        Assert.Equal(
            1,
            authorReferenceCount);

        Assert.Equal(
            1,
            detailsClient.CallCount);
    }

    private WebApplicationFactory<Program>
        CreateApplication(
            IExternalBookDetailsClient detailsClient,
            IExternalBookEditionClient? editionClient = null)
    {
        editionClient ??=
            new StubExternalBookEditionClient(
                null);

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IExternalBookDetailsClient>();

                services.RemoveAll<
                    IExternalBookEditionClient>();

                services.AddSingleton(
                    detailsClient);

                services.AddSingleton(
                    editionClient);
            });
        });
    }

    private sealed class StubExternalBookDetailsClient(
        ExternalBookDetails? result)
        : IExternalBookDetailsClient
    {
        public int CallCount { get; private set; }

        public string? ExternalId
        {
            get;
            private set;
        }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        public Task<ExternalBookDetails?> GetByIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ExternalId = externalId;
            CancellationToken = cancellationToken;

            return Task.FromResult(
                result);
        }
    }

    private sealed class StubExternalBookEditionClient(
        ExternalBookEditionDetails? result)
        : IExternalBookEditionClient
    {
        public int CallCount { get; private set; }

        public string? ExternalWorkId
        {
            get;
            private set;
        }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        public Task<ExternalBookEditionDetails?>
            GetBestForWorkAsync(
                string externalWorkId,
                CancellationToken cancellationToken = default)
        {
            CallCount++;
            ExternalWorkId = externalWorkId;
            CancellationToken = cancellationToken;

            ExternalBookEditionDetails resolvedResult =
                result ??
                CreateTurkishEdition(
                    externalWorkId);

            return Task.FromResult<
                ExternalBookEditionDetails?>(
                    resolvedResult);
        }

        private static ExternalBookEditionDetails
            CreateTurkishEdition(
                string externalWorkId)
        {
            string editionExternalId =
                externalWorkId.Replace(
                    "/works/",
                    "/books/",
                    StringComparison.OrdinalIgnoreCase);

            if (editionExternalId.EndsWith(
                    "W",
                    StringComparison.OrdinalIgnoreCase))
            {
                editionExternalId =
                    $"{editionExternalId[..^1]}M";
            }

            return new ExternalBookEditionDetails(
                "OpenLibrary",
                editionExternalId,
                "Türkçe Test Baskısı",
                "tur",
                "Paperback",
                null,
                null,
                320,
                2024,
                null,
                null);
        }
    }

    private sealed class
    StubMultipleExternalBookEditionClient(
        IReadOnlyList<
            ExternalBookEditionDetails> results)
    : IExternalBookEditionClient
    {
        public int CallCount { get; private set; }

        public string? ExternalWorkId
        {
            get;
            private set;
        }

        public CancellationToken CancellationToken
        {
            get;
            private set;
        }

        public Task<ExternalBookEditionDetails?>
            GetBestForWorkAsync(
                string externalWorkId,
                CancellationToken cancellationToken = default)
        {
            CallCount++;
            ExternalWorkId = externalWorkId;
            CancellationToken = cancellationToken;

            return Task.FromResult(
                results.FirstOrDefault());
        }

        public Task<
            IReadOnlyList<ExternalBookEditionDetails>>
            GetBestEditionsForWorkAsync(
                string externalWorkId,
                CancellationToken cancellationToken = default)
        {
            CallCount++;
            ExternalWorkId = externalWorkId;
            CancellationToken = cancellationToken;

            return Task.FromResult(results);
        }
    }
}