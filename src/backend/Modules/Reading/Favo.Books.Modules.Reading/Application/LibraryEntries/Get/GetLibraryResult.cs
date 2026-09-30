using Favo.Books.Modules.Reading.Domain.LibraryEntries;

namespace Favo.Books.Modules.Reading
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