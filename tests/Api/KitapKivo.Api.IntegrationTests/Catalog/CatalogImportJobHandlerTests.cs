using KitapKivo.Modules.Catalog.Application
    .Imports.Create;
using KitapKivo.Modules.Catalog.Application
    .Imports.GetById;
using KitapKivo.Modules.Catalog.Domain.Imports;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Api.IntegrationTests.Catalog;

public sealed class CatalogImportJobHandlerTests :
    IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly KitapKivoWebApplicationFactory
        _factory;

    public CatalogImportJobHandlerTests(
        KitapKivoWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateHandleAsync_ShouldPersistPendingJob()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        CreateCatalogImportJobHandler handler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        CatalogDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        string query =
            $"catalog-import-{Guid.NewGuid():N}";

        CreateCatalogImportJobCommand command = new(
            " openlibrary ",
            query,
            StartPage: 2,
            PageSize: 10,
            PageCount: 5);

        // Act
        CreateCatalogImportJobResult result =
            await handler.HandleAsync(command);

        // Assert
        CatalogImportJob job =
            await dbContext.ImportJobs
                .AsNoTracking()
                .SingleAsync(importJob =>
                    importJob.Id == result.JobId);

        Assert.Equal(
            CatalogImportJobStatus.Pending,
            job.Status);

        Assert.Equal("OpenLibrary", job.Provider);
        Assert.Equal(query, job.Query);
        Assert.Equal(2, job.StartPage);
        Assert.Equal(2, job.NextPage);
        Assert.Equal(10, job.PageSize);
        Assert.Equal(5, job.PageCount);
        Assert.Equal(0, job.CompletedPageCount);
        Assert.Equal(0, job.ProcessedCount);
        Assert.Null(job.TotalAvailableCount);
        Assert.Null(job.StartedAtUtc);
        Assert.Null(job.CompletedAtUtc);

        Assert.Equal("Pending", result.Status);
        Assert.NotEqual(
            default,
            result.CreatedAtUtc);
    }

    [Fact]
    public async Task CreateHandleAsync_WhenProviderIsUnsupported_ShouldThrow()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        CreateCatalogImportJobHandler handler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        CreateCatalogImportJobCommand command = new(
            "UnsupportedProvider",
            "The Hobbit",
            StartPage: 1,
            PageSize: 10,
            PageCount: 1);

        // Act
        Task Act()
        {
            return handler.HandleAsync(command);
        }

        // Assert
        ArgumentException exception =
            await Assert.ThrowsAsync<
                ArgumentException>(Act);

        Assert.Contains(
            "OpenLibrary",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetHandleAsync_WhenJobExists_ShouldReturnJob()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        CreateCatalogImportJobHandler createHandler =
            scope.ServiceProvider.GetRequiredService<
                CreateCatalogImportJobHandler>();

        GetCatalogImportJobByIdHandler getHandler =
            scope.ServiceProvider.GetRequiredService<
                GetCatalogImportJobByIdHandler>();

        string query =
            $"get-job-{Guid.NewGuid():N}";

        CreateCatalogImportJobResult created =
            await createHandler.HandleAsync(
                new CreateCatalogImportJobCommand(
                    "OpenLibrary",
                    query,
                    StartPage: 3,
                    PageSize: 15,
                    PageCount: 4));

        // Act
        GetCatalogImportJobByIdResult? result =
            await getHandler.HandleAsync(
                new GetCatalogImportJobByIdQuery(
                    created.JobId));

        // Assert
        Assert.NotNull(result);

        Assert.Equal(created.JobId, result.JobId);
        Assert.Equal("OpenLibrary", result.Provider);
        Assert.Equal(query, result.Query);
        Assert.Equal(3, result.StartPage);
        Assert.Equal(3, result.NextPage);
        Assert.Equal(15, result.PageSize);
        Assert.Equal(4, result.PageCount);
        Assert.Equal(0, result.CompletedPageCount);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.ExistingCount);
        Assert.Equal(0, result.NotFoundCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Null(result.LastError);
        Assert.Null(result.NextAttemptAtUtc);
    }

    [Fact]
    public async Task GetHandleAsync_WhenJobDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        using IServiceScope scope =
            _factory.Services.CreateScope();

        GetCatalogImportJobByIdHandler handler =
            scope.ServiceProvider.GetRequiredService<
                GetCatalogImportJobByIdHandler>();

        GetCatalogImportJobByIdQuery query = new(
            Guid.NewGuid());

        // Act
        GetCatalogImportJobByIdResult? result =
            await handler.HandleAsync(query);

        // Assert
        Assert.Null(result);
    }
}