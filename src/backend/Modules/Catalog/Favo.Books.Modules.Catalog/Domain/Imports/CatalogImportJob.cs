namespace Favo.Books.Modules.Catalog.Domain.Imports;

internal sealed class CatalogImportJob
{
    private const int MaximumProviderLength = 50;

    private const int MaximumQueryLength = 200;

    private const int MaximumBatchSize = 20;

    private const int MaximumPageCount = 500;

    private const int MaximumErrorLength = 2_000;

    private CatalogImportJob()
    {
    }

    public CatalogImportJob(
        string provider,
        string query,
        int startPage,
        int pageSize,
        int pageCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            provider);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            query);

        string normalizedProvider = provider.Trim();
        string normalizedQuery = query.Trim();

        if (normalizedProvider.Length >
            MaximumProviderLength)
        {
            throw new ArgumentException(
                $"Provider cannot exceed " +
                $"{MaximumProviderLength} characters.",
                nameof(provider));
        }

        if (normalizedQuery.Length >
            MaximumQueryLength)
        {
            throw new ArgumentException(
                $"Query cannot exceed " +
                $"{MaximumQueryLength} characters.",
                nameof(query));
        }

        if (startPage < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startPage));
        }

        if (pageSize is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize));
        }

        if (pageCount is < 1 or > MaximumPageCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageCount));
        }

        if (startPage > int.MaxValue - pageCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startPage));
        }

        Id = Guid.NewGuid();
        Provider = normalizedProvider;
        Query = normalizedQuery;
        StartPage = startPage;
        NextPage = startPage;
        PageSize = pageSize;
        PageCount = pageCount;
        Status = CatalogImportJobStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string Provider { get; private set; } =
        string.Empty;

    public string Query { get; private set; } =
        string.Empty;

    public int StartPage { get; private set; }

    public int NextPage { get; private set; }

    public int PageSize { get; private set; }

    public int PageCount { get; private set; }

    public int CompletedPageCount { get; private set; }

    public CatalogImportJobStatus Status
    {
        get;
        private set;
    }

    public int? TotalAvailableCount { get; private set; }

    public int ProcessedCount { get; private set; }

    public int CreatedCount { get; private set; }

    public int ExistingCount { get; private set; }

    public int NotFoundCount { get; private set; }

    public int FailedCount { get; private set; }

    public int ConsecutiveFailureCount
    {
        get;
        private set;
    }

    public string? LastError { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset CreatedAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset UpdatedAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset? StartedAtUtc
    {
        get;
        private set;
    }

    public DateTimeOffset? CompletedAtUtc
    {
        get;
        private set;
    }

    public void Start(DateTimeOffset startedAtUtc)
    {
        if (Status is not
            CatalogImportJobStatus.Pending and not
            CatalogImportJobStatus.WaitingForRetry)
        {
            throw new InvalidOperationException(
                "Only pending or retrying import jobs " +
                "can be started.");
        }

        if (Status ==
                CatalogImportJobStatus.WaitingForRetry &&
            NextAttemptAtUtc.HasValue &&
            NextAttemptAtUtc.Value > startedAtUtc)
        {
            throw new InvalidOperationException(
                "The import job retry time has not arrived.");
        }

        Status = CatalogImportJobStatus.Running;
        StartedAtUtc ??= startedAtUtc;
        NextAttemptAtUtc = null;
        LastError = null;
        UpdatedAtUtc = startedAtUtc;
    }

    public void RecordCompletedPage(
        int totalAvailableCount,
        int processedCount,
        int createdCount,
        int existingCount,
        int notFoundCount,
        int failedCount,
        DateTimeOffset completedAtUtc)
    {
        EnsureRunning();

        if (totalAvailableCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalAvailableCount));
        }

        if (processedCount < 0 ||
            createdCount < 0 ||
            existingCount < 0 ||
            notFoundCount < 0 ||
            failedCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processedCount),
                "Import counts cannot be negative.");
        }

        if (processedCount !=
            createdCount +
            existingCount +
            notFoundCount +
            failedCount)
        {
            throw new ArgumentException(
                "Processed count must equal the sum " +
                "of all import outcomes.",
                nameof(processedCount));
        }

        TotalAvailableCount ??= totalAvailableCount;
        ProcessedCount += processedCount;
        CreatedCount += createdCount;
        ExistingCount += existingCount;
        NotFoundCount += notFoundCount;
        FailedCount += failedCount;
        CompletedPageCount++;
        NextPage++;
        ConsecutiveFailureCount = 0;
        NextAttemptAtUtc = null;
        LastError = null;
        UpdatedAtUtc = completedAtUtc;

        if (CompletedPageCount >= PageCount)
        {
            Complete(completedAtUtc);
            return;
        }

        Status = CatalogImportJobStatus.Pending;
    }

    public void ScheduleRetry(
        string error,
        DateTimeOffset nextAttemptAtUtc,
        DateTimeOffset occurredAtUtc)
    {
        EnsureRunning();

        ArgumentException.ThrowIfNullOrWhiteSpace(
            error);

        if (nextAttemptAtUtc <= occurredAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nextAttemptAtUtc));
        }

        Status =
            CatalogImportJobStatus.WaitingForRetry;

        ConsecutiveFailureCount++;
        LastError = NormalizeError(error);
        NextAttemptAtUtc = nextAttemptAtUtc;
        UpdatedAtUtc = occurredAtUtc;
    }

    public void RequeueInterrupted(
        DateTimeOffset requeuedAtUtc)
    {
        if (Status != CatalogImportJobStatus.Running)
        {
            throw new InvalidOperationException(
                "Only running import jobs can be requeued.");
        }

        Status = CatalogImportJobStatus.Pending;
        NextAttemptAtUtc = null;
        UpdatedAtUtc = requeuedAtUtc;
    }

    public void Complete(
        DateTimeOffset completedAtUtc)
    {
        if (Status is
            CatalogImportJobStatus.Completed or
            CatalogImportJobStatus.CompletedWithFailures or
            CatalogImportJobStatus.Failed)
        {
            throw new InvalidOperationException(
                "The import job is already completed.");
        }

        Status = FailedCount > 0
            ? CatalogImportJobStatus
                .CompletedWithFailures
            : CatalogImportJobStatus.Completed;

        NextAttemptAtUtc = null;
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;
    }

    public void Fail(
        string error,
        DateTimeOffset failedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            error);

        if (Status is
            CatalogImportJobStatus.Completed or
            CatalogImportJobStatus.CompletedWithFailures or
            CatalogImportJobStatus.Failed)
        {
            throw new InvalidOperationException(
                "A completed import job cannot fail.");
        }

        Status = CatalogImportJobStatus.Failed;
        LastError = NormalizeError(error);
        NextAttemptAtUtc = null;
        CompletedAtUtc = failedAtUtc;
        UpdatedAtUtc = failedAtUtc;
    }

    private void EnsureRunning()
    {
        if (Status != CatalogImportJobStatus.Running)
        {
            throw new InvalidOperationException(
                "The import job must be running.");
        }
    }

    private static string NormalizeError(
        string error)
    {
        string normalizedError = error.Trim();

        return normalizedError.Length <=
               MaximumErrorLength
            ? normalizedError
            : normalizedError[..MaximumErrorLength];
    }
}