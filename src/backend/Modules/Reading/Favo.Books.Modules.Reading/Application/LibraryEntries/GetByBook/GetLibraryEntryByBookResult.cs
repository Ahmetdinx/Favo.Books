using Favo.Books.Modules.Reading
    .Domain.LibraryEntries;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.GetByBook;

internal sealed record GetLibraryEntryByBookResult(
    Guid LibraryEntryId,
    Guid BookId,
    LibraryEntryStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);