using KitapKivo.Modules.Catalog.Domain.Imports;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application
    .Imports.GetById;

public sealed class GetCatalogImportJobByIdHandler
{
    private readonly CatalogDbContext _dbContext;

    public GetCatalogImportJobByIdHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetCatalogImportJobByIdResult?>
        HandleAsync(
            GetCatalogImportJobByIdQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.JobId == Guid.Empty)
        {
            throw new ArgumentException(
                "Import job identifier cannot be empty.",
                nameof(query));
        }

        CatalogImportJob? job =
            await _dbContext.ImportJobs
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    importJob =>
                        importJob.Id == query.JobId,
                    cancellationToken);

        if (job is null)
        {
            return null;
        }

        return new GetCatalogImportJobByIdResult(
            job.Id,
            job.Provider,
            job.Query,
            job.StartPage,
            job.NextPage,
            job.PageSize,
            job.PageCount,
            job.CompletedPageCount,
            job.Status.ToString(),
            job.TotalAvailableCount,
            job.ProcessedCount,
            job.CreatedCount,
            job.ExistingCount,
            job.NotFoundCount,
            job.FailedCount,
            job.ConsecutiveFailureCount,
            job.LastError,
            job.NextAttemptAtUtc,
            job.CreatedAtUtc,
            job.UpdatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc);
    }
}