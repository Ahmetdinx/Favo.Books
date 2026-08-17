namespace KitapKivo.Modules.Catalog.Presentation
    .Imports.GetById;

internal sealed record GetCatalogImportJobByIdResponse(
    Guid JobId,
    string Provider,
    string Query,
    int StartPage,
    int NextPage,
    int PageSize,
    int PageCount,
    int CompletedPageCount,
    string Status,
    int? TotalAvailableCount,
    int ProcessedCount,
    int CreatedCount,
    int ExistingCount,
    int NotFoundCount,
    int FailedCount,
    int ConsecutiveFailureCount,
    string? LastError,
    DateTimeOffset? NextAttemptAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);