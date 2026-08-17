namespace KitapKivo.Modules.Catalog.Infrastructure
    .Imports;

internal sealed class CatalogImportJobWorkerOptions
{
    public const string SectionName =
        "Catalog:ImportJobs";

    public bool WorkerEnabled { get; init; } = true;

    public int PollIntervalSeconds { get; init; } = 5;

    public int DelayBetweenPagesSeconds { get; init; } = 2;

    public int MaximumRetryCount { get; init; } = 5;

    public int InitialRetryDelaySeconds { get; init; } = 15;

    public int MaximumRetryDelaySeconds { get; init; } = 900;

    public int InterruptedJobTimeoutMinutes { get; init; } = 60;
}