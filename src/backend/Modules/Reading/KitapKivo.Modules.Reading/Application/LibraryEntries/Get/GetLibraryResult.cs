using KitapKivo.Modules.Reading.Domain.LibraryEntries;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Get;

internal sealed record GetLibraryResult(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<GetLibraryItemResult> Items);

internal sealed record GetLibraryItemResult(
    Guid LibraryEntryId,
    Guid BookId,
    LibraryEntryStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);