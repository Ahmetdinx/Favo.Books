using Docker.DotNet.Models;

using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;
using KitapKivo.Modules.Catalog.Domain.Authors;
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
    private readonly KitapKivoWebApplicationFactory _factory;

    public ResolveExternalBookHandlerTests(
        KitapKivoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HandleAsync_WhenBookDoesNotExist_ShouldCreateBook()
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
            new StubExternalBookDetailsClient(details);

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
        Assert.True(result.IsSuccess);
        Assert.True(result.IsCreated);
        Assert.NotNull(result.BookId);

        Assert.True(
            await dbContext.Books
                .AsNoTracking()
                .AnyAsync(
                    book => book.Id == result.BookId.Value));

        Assert.True(
            await dbContext.ExternalReferences
                .AsNoTracking()
                .AnyAsync(reference =>
                    reference.BookId == result.BookId.Value &&
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/works/OL10001W"));

        Guid authorId =
            await dbContext.Books
                .AsNoTracking()
                .Where(
                    book =>
                        book.Id == result.BookId.Value)
                .SelectMany(book => book.Authors)
                .Select(bookAuthor => bookAuthor.AuthorId)
                .SingleAsync();

        Assert.True(
            await dbContext.Authors
                .AsNoTracking()
                .AnyAsync(author =>
                    author.Id == authorId &&
                    author.Name == "Frank Herbert"));

        Assert.True(
            await dbContext.ExternalReferences
                .AsNoTracking()
                .AnyAsync(reference =>
                    reference.AuthorId == authorId &&
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/authors/OL20001A"));

        Assert.Equal(1, detailsClient.CallCount);

        Assert.Equal(
            "/works/OL10001W",
            detailsClient.ExternalId);

        Assert.Equal(
            cancellationTokenSource.Token,
            detailsClient.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_WhenBookAlreadyExists_ShouldReturnExistingBook()
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
            new StubExternalBookDetailsClient(details);

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

        var command = new ResolveExternalBookCommand(
            "OpenLibrary",
            "/works/OL10002W");

        // Act
        ResolveExternalBookResult firstResult =
            await handler.HandleAsync(command);

        ResolveExternalBookResult secondResult =
            await handler.HandleAsync(command);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(firstResult.IsCreated);

        Assert.True(secondResult.IsSuccess);
        Assert.False(secondResult.IsCreated);

        Assert.Equal(
            firstResult.BookId,
            secondResult.BookId);

        Assert.Equal(1, detailsClient.CallCount);

        int referenceCount =
            await dbContext.ExternalReferences
                .AsNoTracking()
                .CountAsync(reference =>
                    reference.Source ==
                        CatalogSource.OpenLibrary &&
                    reference.ExternalId ==
                        "/works/OL10002W" &&
                    reference.BookId.HasValue);

        Assert.Equal(1, referenceCount);
    }

    [Fact]
    public async Task HandleAsync_WhenExternalBookDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(null);

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
        Assert.False(result.IsSuccess);
        Assert.False(result.IsCreated);
        Assert.Null(result.BookId);

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

        Assert.Equal(1, detailsClient.CallCount);
    }

    [Fact]
    public async Task HandleAsync_WhenAuthorAlreadyExists_ShouldReuseAuthor()
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
            new StubExternalBookDetailsClient(details);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        var existingAuthor =
            new Author("Existing Author");

        existingAuthor.ExternalReferences.Add(
            ExternalReference.CreateForAuthor(
                existingAuthor.Id,
                CatalogSource.OpenLibrary,
                "/authors/OL20004A"));

        dbContext.Authors.Add(existingAuthor);

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
        Assert.True(result.IsSuccess);
        Assert.True(result.IsCreated);
        Assert.NotNull(result.BookId);

        Guid linkedAuthorId =
            await dbContext.Books
                .AsNoTracking()
                .Where(
                    book =>
                        book.Id == result.BookId.Value)
                .SelectMany(book => book.Authors)
                .Select(bookAuthor => bookAuthor.AuthorId)
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

        Assert.Equal(1, authorReferenceCount);
        Assert.Equal(1, detailsClient.CallCount);
    }

    private WebApplicationFactory<Program> CreateApplication(
        IExternalBookDetailsClient detailsClient)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IExternalBookDetailsClient>();

                services.AddSingleton(detailsClient);
            });
        });
    }

    private sealed class StubExternalBookDetailsClient(
        ExternalBookDetails? result)
        : IExternalBookDetailsClient
    {
        public int CallCount { get; private set; }

        public string? ExternalId { get; private set; }

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

            return Task.FromResult(result);
        }
    }
}