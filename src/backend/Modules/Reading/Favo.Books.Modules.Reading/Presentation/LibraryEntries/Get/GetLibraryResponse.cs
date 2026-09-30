namespace Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.Get;

public sealed record GetLibraryResponse(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<GetLibraryItemResponse> Items);

public sealed record GetLibraryItemResponse(
    Guid LibraryEntryId,
    Guid BookId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);