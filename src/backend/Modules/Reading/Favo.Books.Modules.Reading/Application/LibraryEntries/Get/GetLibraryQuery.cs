using Favo.Books.Modules.Reading.Domain.LibraryEntries;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.Get;

internal sealed record GetLibraryQuery(
    Guid UserId,
    LibraryEntryStatus? Status,
    int Page,
    int PageSize);