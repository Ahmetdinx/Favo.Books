namespace KitapKivo.Modules.Catalog.Presentation
    .Books.ImportExternal;

internal sealed record ImportExternalBooksResponse(
    int TotalCount,
    int Page,
    int PageSize,
    int FoundCount,
    int ProcessedCount,
    int CreatedCount,
    int ExistingCount,
    int NotFoundCount,
    int FailedCount,
    IReadOnlyList<
        ImportExternalBookFailureResponse> Failures);

internal sealed record ImportExternalBookFailureResponse(
    string ExternalId,
    string Title,
    string Reason);