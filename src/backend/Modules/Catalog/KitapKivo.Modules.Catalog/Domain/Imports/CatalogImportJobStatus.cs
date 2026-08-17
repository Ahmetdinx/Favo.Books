namespace KitapKivo.Modules.Catalog.Domain.Imports;

internal enum CatalogImportJobStatus
{
    Pending = 0,
    Running = 1,
    WaitingForRetry = 2,
    Completed = 3,
    CompletedWithFailures = 4,
    Failed = 5
}