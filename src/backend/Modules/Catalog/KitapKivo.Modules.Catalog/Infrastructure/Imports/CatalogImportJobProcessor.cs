using KitapKivo.Modules.Catalog.Application
    .Books.ImportExternal;
using KitapKivo.Modules.Catalog.Domain.Imports;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KitapKivo.Modules.Catalog.Infrastructure
    .Imports;

internal sealed class CatalogImportJobProcessor
{
    private readonly CatalogDbContext _dbContext;

    private readonly ImportExternalBooksHandler
        _importHandler;

    private readonly CatalogImportJobWorkerOptions
        _options;

    private readonly ILogger<CatalogImportJobProcessor>
        _logger;

    public CatalogImportJobProcessor(
        CatalogDbContext dbContext,
        ImportExternalBooksHandler importHandler,
        IOptions<CatalogImportJobWorkerOptions> options,
        ILogger<CatalogImportJobProcessor> logger)
    {
        _dbContext = dbContext;
        _importHandler = importHandler;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> ProcessNextAsync(
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset claimedAtUtc =
            DateTimeOffset.UtcNow;

        CatalogImportJob? job =
            await ClaimNextJobAsync(
                claimedAtUtc,
                cancellationToken);

        if (job is null)
        {
            return false;
        }

        try
        {
            ImportExternalBooksResult result =
                await _importHandler.HandleAsync(
                    new ImportExternalBooksCommand(
                        job.Provider,
                        job.Query,
                        job.NextPage,
                        job.PageSize),
                    cancellationToken);

            DateTimeOffset completedAtUtc =
                DateTimeOffset.UtcNow;

            job.RecordCompletedPage(
                result.TotalCount,
                result.ProcessedCount,
                result.CreatedCount,
                result.ExistingCount,
                result.NotFoundCount,
                result.FailedCount,
                completedAtUtc);

            if (job.Status ==
                    CatalogImportJobStatus.Pending &&
                HasReachedEndOfResults(result))
            {
                job.Complete(completedAtUtc);
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogInformation(
                "Catalog import job {JobId} processed " +
                "page {Page}. Current status: {Status}.",
                job.Id,
                result.Page,
                job.Status);

            return true;
        }
        catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested)
        {
            job.RequeueInterrupted(
                DateTimeOffset.UtcNow);

            await _dbContext.SaveChangesAsync(
                CancellationToken.None);

            throw;
        }
        catch (OperationCanceledException exception)
        {
            await HandleTransientFailureAsync(
                job,
                exception,
                cancellationToken);

            return true;
        }
        catch (HttpRequestException exception)
        {
            await HandleTransientFailureAsync(
                job,
                exception,
                cancellationToken);

            return true;
        }
        catch (Exception exception)
        {
            await HandleFatalFailureAsync(
                job,
                exception,
                cancellationToken);

            return true;
        }
    }

    public async Task<int>
        RequeueInterruptedJobsAsync(
            CancellationToken cancellationToken = default)
    {
        DateTimeOffset requeuedAtUtc =
            DateTimeOffset.UtcNow;

        DateTimeOffset staleBeforeUtc =
            requeuedAtUtc.AddMinutes(
                -_options.InterruptedJobTimeoutMinutes);

        CatalogImportJob[] interruptedJobs =
            await _dbContext.ImportJobs
                .Where(job =>
                    job.Status ==
                        CatalogImportJobStatus.Running &&
                    job.UpdatedAtUtc <= staleBeforeUtc)
                .ToArrayAsync(cancellationToken);

        foreach (CatalogImportJob job
                 in interruptedJobs)
        {
            job.RequeueInterrupted(requeuedAtUtc);
        }

        if (interruptedJobs.Length > 0)
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogWarning(
                "{JobCount} interrupted catalog import " +
                "jobs were returned to the queue.",
                interruptedJobs.Length);
        }

        return interruptedJobs.Length;
    }

    private async Task<CatalogImportJob?>
        ClaimNextJobAsync(
            DateTimeOffset claimedAtUtc,
            CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await _dbContext.Database
                .BeginTransactionAsync(
                    cancellationToken);

        List<CatalogImportJob> jobs =
            await _dbContext.ImportJobs
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM catalog."ImportJobs"
                    WHERE
                        "Status" = 'Pending'
                        OR
                        (
                            "Status" = 'WaitingForRetry'
                            AND "NextAttemptAtUtc" <=
                                {claimedAtUtc}
                        )
                    ORDER BY "CreatedAtUtc"
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1
                    """)
                .ToListAsync(cancellationToken);

        CatalogImportJob? job =
            jobs.SingleOrDefault();

        if (job is null)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return null;
        }

        job.Start(claimedAtUtc);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return job;
    }

    private async Task HandleTransientFailureAsync(
        CatalogImportJob job,
        Exception exception,
        CancellationToken cancellationToken)
    {
        DateTimeOffset occurredAtUtc =
            DateTimeOffset.UtcNow;

        string error = ResolveError(exception);

        int nextFailureCount =
            job.ConsecutiveFailureCount + 1;

        if (nextFailureCount >=
            _options.MaximumRetryCount)
        {
            job.Fail(
                error,
                occurredAtUtc);

            _logger.LogError(
                exception,
                "Catalog import job {JobId} failed " +
                "after {FailureCount} attempts.",
                job.Id,
                nextFailureCount);
        }
        else
        {
            TimeSpan retryDelay =
                CalculateRetryDelay(
                    job.ConsecutiveFailureCount);

            DateTimeOffset nextAttemptAtUtc =
                occurredAtUtc.Add(retryDelay);

            job.ScheduleRetry(
                error,
                nextAttemptAtUtc,
                occurredAtUtc);

            _logger.LogWarning(
                exception,
                "Catalog import job {JobId} will be " +
                "retried at {NextAttemptAtUtc}.",
                job.Id,
                nextAttemptAtUtc);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private async Task HandleFatalFailureAsync(
        CatalogImportJob job,
        Exception exception,
        CancellationToken cancellationToken)
    {
        job.Fail(
            ResolveError(exception),
            DateTimeOffset.UtcNow);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        _logger.LogError(
            exception,
            "Catalog import job {JobId} failed " +
            "with a non-retryable error.",
            job.Id);
    }

    private TimeSpan CalculateRetryDelay(
        int consecutiveFailureCount)
    {
        int exponent =
            Math.Min(
                consecutiveFailureCount,
                20);

        double multiplier =
            Math.Pow(2, exponent);

        double delaySeconds =
            _options.InitialRetryDelaySeconds *
            multiplier;

        delaySeconds = Math.Min(
            delaySeconds,
            _options.MaximumRetryDelaySeconds);

        return TimeSpan.FromSeconds(
            delaySeconds);
    }

    private static bool HasReachedEndOfResults(
        ImportExternalBooksResult result)
    {
        if (result.FoundCount == 0)
        {
            return true;
        }

        long consumedResultCount =
            ((long)result.Page - 1) *
            result.PageSize +
            result.FoundCount;

        return consumedResultCount >=
               result.TotalCount;
    }

    private static string ResolveError(
        Exception exception)
    {
        return string.IsNullOrWhiteSpace(
            exception.Message)
            ? exception.GetType().Name
            : exception.Message;
    }
}