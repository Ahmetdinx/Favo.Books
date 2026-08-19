using KitapKivo.Modules.Reading
    .Domain.LibraryEntries;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.GetByBook;

internal sealed record GetLibraryEntryByBookResult(
    Guid LibraryEntryId,
    Guid BookId,
    LibraryEntryStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);