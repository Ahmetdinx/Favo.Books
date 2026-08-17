namespace KitapKivo.Modules.Catalog.Application
    .Books.ImportExternal;

public sealed record ImportExternalBooksResult(
    int TotalCount,
    int Page,
    int PageSize,
    int FoundCount,
    int ProcessedCount,
    int CreatedCount,
    int ExistingCount,
    int NotFoundCount,
    int FailedCount,
    IReadOnlyList<ImportExternalBookFailureResult> Failures);

public sealed record ImportExternalBookFailureResult(
    string ExternalId,
    string Title,
    string Reason);