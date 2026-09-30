using Favo.Books.Modules.Catalog.Application.Books;
using System.Text.Json;

using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application
    .Imports.Create;
using Favo.Books.Modules.Catalog.Domain.Imports;
using Favo.Books.Modules.Catalog.Infrastructure
    .Imports;
using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class CatalogImportJobProcessorTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private readonly FavoBooksWebApplicationFactory
        _factory;

    public CatalogImportJobProcessorTests(
        FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessNextAsync_WithSinglePage_ShouldCompleteJob()
    {
        // Arrange
        const string firstExternalId =
            "/works/OL9100001W";

        const string secondExternalId =
            "/works/OL9100002W";

        ExternalBookSearchItem[] items =
        [
            CreateSearchItem(
                firstExternalId,
                "Worker Test Book One"),
            CreateSearchItem(
                secondExternalId,
                "Worker Test Book Two")
        ];

        var searchClient =
            new StubExternalBookSearchClient(
                (_, page, pageSize) =>
                    new ExternalBookSearchResult(
                        TotalCount: 2,
                        Page: page,
                        PageSize: pageSize,
                        Items: items));

        var detailsClient =
            new StubExternalBookDetailsClient(
                CreateDetails(
                    firstExternalId,
                    "Worker Test Book One"),
                CreateDetails(
                    secondExternalId,
                    "Worker Test Book Two"));

        using WebApplicationFactory<Program> application =
            CreateApplication(
                searchClient,
                detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CreateCatalogImportJobHandler createHandler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        CatalogImportJobProcessor processor =
            scope.ServiceProvider.GetRequiredService<
                CatalogImportJobProcessor>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        CreateCatalogImportJobResult created =
            await createHandler.HandleAsync(
                new CreateCatalogImportJobCommand(
                    "OpenLibrary",
                    "worker-single-page",
                    StartPage: 1,
                    PageSize: 10,
                    PageCount: 5));

        // Act
        bool processed =
            await processor.ProcessNextAsync();

        // Assert
        Assert.True(processed);

        CatalogImportJob job =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(importJob =>
                    importJob.Id == created.JobId);

        Assert.Equal(
            CatalogImportJobStatus.Completed,
            job.Status);

        Assert.Equal(1, job.CompletedPageCount);
        Assert.Equal(2, job.NextPage);
        Assert.Equal(2, job.TotalAvailableCount);
        Assert.Equal(2, job.ProcessedCount);
        Assert.Equal(2, job.CreatedCount);
        Assert.Equal(0, job.ExistingCount);
        Assert.Equal(0, job.NotFoundCount);
        Assert.Equal(0, job.FailedCount);
        Assert.NotNull(job.StartedAtUtc);
        Assert.NotNull(job.CompletedAtUtc);

        string[] externalIds =
        [
            firstExternalId,
            secondExternalId
        ];

        int importedReferenceCount =
            await dbContext.ExternalReferences
                .AsNoTracking()
                .CountAsync(reference =>
                    externalIds.Contains(
                        reference.ExternalId));

        Assert.Equal(2, importedReferenceCount);
    }

    [Fact]
    public async Task ProcessNextAsync_WithMultiplePages_ShouldContinueFromNextPage()
    {
        // Arrange
        const string firstExternalId =
            "/works/OL9200001W";

        const string secondExternalId =
            "/works/OL9200002W";

        var searchClient =
            new StubExternalBookSearchClient(
                (_, page, pageSize) =>
                {
                    ExternalBookSearchItem[] items =
                        page switch
                        {
                            1 =>
                            [
                                CreateSearchItem(
                                    firstExternalId,
                                    "Paged Worker Book One")
                            ],
                            2 =>
                            [
                                CreateSearchItem(
                                    secondExternalId,
                                    "Paged Worker Book Two")
                            ],
                            _ => []
                        };

                    return new ExternalBookSearchResult(
                        TotalCount: 2,
                        Page: page,
                        PageSize: pageSize,
                        Items: items);
                });

        var detailsClient =
            new StubExternalBookDetailsClient(
                CreateDetails(
                    firstExternalId,
                    "Paged Worker Book One"),
                CreateDetails(
                    secondExternalId,
                    "Paged Worker Book Two"));

        using WebApplicationFactory<Program> application =
            CreateApplication(
                searchClient,
                detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CreateCatalogImportJobHandler createHandler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        CatalogImportJobProcessor processor =
            scope.ServiceProvider.GetRequiredService<
                CatalogImportJobProcessor>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        CreateCatalogImportJobResult created =
            await createHandler.HandleAsync(
                new CreateCatalogImportJobCommand(
                    "OpenLibrary",
                    "worker-multiple-pages",
                    StartPage: 1,
                    PageSize: 1,
                    PageCount: 2));

        // Act
        bool firstPageProcessed =
            await processor.ProcessNextAsync();

        CatalogImportJob firstPageJob =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(importJob =>
                    importJob.Id == created.JobId);

        bool secondPageProcessed =
            await processor.ProcessNextAsync();

        CatalogImportJob completedJob =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(importJob =>
                    importJob.Id == created.JobId);

        // Assert
        Assert.True(firstPageProcessed);

        Assert.Equal(
            CatalogImportJobStatus.Pending,
            firstPageJob.Status);

        Assert.Equal(
            1,
            firstPageJob.CompletedPageCount);

        Assert.Equal(2, firstPageJob.NextPage);

        Assert.True(secondPageProcessed);

        Assert.Equal(
            CatalogImportJobStatus.Completed,
            completedJob.Status);

        Assert.Equal(
            2,
            completedJob.CompletedPageCount);

        Assert.Equal(3, completedJob.NextPage);
        Assert.Equal(2, completedJob.ProcessedCount);
        Assert.Equal(2, completedJob.CreatedCount);
        Assert.NotNull(completedJob.CompletedAtUtc);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenProviderTemporarilyFails_ShouldScheduleRetry()
    {
        // Arrange
        var searchClient =
            new StubExternalBookSearchClient(
                (_, _, _) =>
                    throw new HttpRequestException(
                        "OpenLibrary is temporarily unavailable."));

        var detailsClient =
            new StubExternalBookDetailsClient();

        using WebApplicationFactory<Program> application =
            CreateApplication(
                searchClient,
                detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CreateCatalogImportJobHandler createHandler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        CatalogImportJobProcessor processor =
            scope.ServiceProvider.GetRequiredService<
                CatalogImportJobProcessor>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        CreateCatalogImportJobResult created =
            await createHandler.HandleAsync(
                new CreateCatalogImportJobCommand(
                    "OpenLibrary",
                    "worker-retry",
                    StartPage: 1,
                    PageSize: 10,
                    PageCount: 1));

        // Act
        bool processed =
            await processor.ProcessNextAsync();

        // Assert
        Assert.True(processed);

        CatalogImportJob job =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(importJob =>
                    importJob.Id == created.JobId);

        Assert.Equal(
            CatalogImportJobStatus.WaitingForRetry,
            job.Status);

        Assert.Equal(
            1,
            job.ConsecutiveFailureCount);

        Assert.Equal(1, job.NextPage);
        Assert.Equal(0, job.CompletedPageCount);
        Assert.Equal(0, job.ProcessedCount);

        Assert.Contains(
            "temporarily unavailable",
            job.LastError,
            StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(job.NextAttemptAtUtc);

        Assert.True(
            job.NextAttemptAtUtc >
            job.UpdatedAtUtc);

        Assert.Null(job.CompletedAtUtc);
    }

    [Fact]
    public async Task ProcessNextAsync_WhenProviderReturnsInvalidData_ShouldFailJob()
    {
        // Arrange
        var searchClient =
            new StubExternalBookSearchClient(
                (_, _, _) =>
                    throw new JsonException(
                        "OpenLibrary returned invalid JSON."));

        var detailsClient =
            new StubExternalBookDetailsClient();

        using WebApplicationFactory<Program> application =
            CreateApplication(
                searchClient,
                detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CreateCatalogImportJobHandler createHandler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        CatalogImportJobProcessor processor =
            scope.ServiceProvider.GetRequiredService<
                CatalogImportJobProcessor>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        CreateCatalogImportJobResult created =
            await createHandler.HandleAsync(
                new CreateCatalogImportJobCommand(
                    "OpenLibrary",
                    "worker-invalid-data",
                    StartPage: 1,
                    PageSize: 10,
                    PageCount: 1));

        // Act
        bool processed =
            await processor.ProcessNextAsync();

        // Assert
        Assert.True(processed);

        CatalogImportJob job =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(importJob =>
                    importJob.Id == created.JobId);

        Assert.Equal(
            CatalogImportJobStatus.Failed,
            job.Status);

        Assert.Contains(
            "invalid JSON",
            job.LastError,
            StringComparison.OrdinalIgnoreCase);

        Assert.Null(job.NextAttemptAtUtc);
        Assert.NotNull(job.CompletedAtUtc);
    }


    [Fact]
    public async Task ProcessNextAsync_WhenBooksAlreadyImported_ShouldCountAsExisting()
    {
        // Arrange
        const string externalId =
            "/works/OL9300001W";

        ExternalBookSearchItem item =
            CreateSearchItem(
                externalId,
                "Idempotent Worker Book");

        var searchClient =
            new StubExternalBookSearchClient(
                (_, page, pageSize) =>
                    new ExternalBookSearchResult(
                        TotalCount: 1,
                        Page: page,
                        PageSize: pageSize,
                        Items: [item]));

        var detailsClient =
            new StubExternalBookDetailsClient(
                CreateDetails(
                    externalId,
                    "Idempotent Worker Book"));

        using WebApplicationFactory<Program> application =
            CreateApplication(
                searchClient,
                detailsClient);

        using IServiceScope scope =
            application.Services.CreateScope();

        CreateCatalogImportJobHandler createHandler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        CatalogImportJobProcessor processor =
            scope.ServiceProvider.GetRequiredService<
                CatalogImportJobProcessor>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        CreateCatalogImportJobResult firstJob =
            await createHandler.HandleAsync(
                new CreateCatalogImportJobCommand(
                    "OpenLibrary",
                    "idempotent-first-import",
                    StartPage: 1,
                    PageSize: 1,
                    PageCount: 1));

        await processor.ProcessNextAsync();

        CreateCatalogImportJobResult secondJob =
            await createHandler.HandleAsync(
                new CreateCatalogImportJobCommand(
                    "OpenLibrary",
                    "idempotent-second-import",
                    StartPage: 1,
                    PageSize: 1,
                    PageCount: 1));

        // Act
        await processor.ProcessNextAsync();

        // Assert
        CatalogImportJob firstImport =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(job =>
                    job.Id == firstJob.JobId);

        CatalogImportJob secondImport =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(job =>
                    job.Id == secondJob.JobId);

        Assert.Equal(
            CatalogImportJobStatus.Completed,
            firstImport.Status);

        Assert.Equal(1, firstImport.CreatedCount);
        Assert.Equal(0, firstImport.ExistingCount);

        Assert.Equal(
            CatalogImportJobStatus.Completed,
            secondImport.Status);

        Assert.Equal(0, secondImport.CreatedCount);
        Assert.Equal(1, secondImport.ExistingCount);
        Assert.Equal(1, secondImport.ProcessedCount);
        Assert.Equal(0, secondImport.FailedCount);

        int referenceCount =
            await dbContext.ExternalReferences
                .AsNoTracking()
                .CountAsync(reference =>
                    reference.ExternalId == externalId);

        Assert.Equal(1, referenceCount);
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
                services.RemoveAll<IApprovedNovelCatalog>();
                services.AddSingleton<IApprovedNovelCatalog>(new FixtureNovelCatalog((IFixtureNovelDetails)detailsClient));

                services.RemoveAll<
                    IExternalBookSearchClient>();

                services.RemoveAll<
                    IExternalBookDetailsClient>();

                services.AddSingleton(
                    searchClient);

                services.AddSingleton(
                    detailsClient);
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
            Authors: [new ExternalBookAuthor(null, "Test Author")],
            FirstPublishedYear: 2000,
            CoverUrl: null,
            Isbns: [],
            LanguageCodes: ["eng"]);
    }

    private static ExternalBookDetails
        CreateDetails(
            string externalId,
            string title)
    {
        return new ExternalBookDetails(
            "OpenLibrary",
            externalId,
            title,
            Description: null,
            Authors: [new ExternalBookAuthor(null, "Test Author")],
            FirstPublishedYear: 2000,
            CoverUrl: null,
            Isbns: [],
            LanguageCodes: ["eng"],
            Subjects: []);
    }

    private sealed class
        StubExternalBookSearchClient :
        IExternalBookSearchClient
    {
        private readonly Func<
            string,
            int,
            int,
            ExternalBookSearchResult> _resultFactory;

        public StubExternalBookSearchClient(
            Func<
                string,
                int,
                int,
                ExternalBookSearchResult> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public Task<ExternalBookSearchResult>
            SearchAsync(
                string query,
                int page,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            ExternalBookSearchResult result =
                _resultFactory(
                    query,
                    page,
                    pageSize);

            return Task.FromResult(result);
        }
    }

    private sealed class
        StubExternalBookDetailsClient :
        IExternalBookDetailsClient, IFixtureNovelDetails
    {
        public ExternalBookDetails? Fixture(string id) => _details.GetValueOrDefault(id);
        private readonly Dictionary<
            string,
            ExternalBookDetails> _details;

        public StubExternalBookDetailsClient(
            params ExternalBookDetails[] details)
        {
            _details = details.ToDictionary(
                detail => detail.ExternalId,
                StringComparer.OrdinalIgnoreCase);
        }

        public Task<ExternalBookDetails?>
            GetByIdAsync(
                string externalId,
                CancellationToken cancellationToken = default)
        {
            _details.TryGetValue(
                externalId,
                out ExternalBookDetails? details);

            return Task.FromResult(details);
        }
    }
}
