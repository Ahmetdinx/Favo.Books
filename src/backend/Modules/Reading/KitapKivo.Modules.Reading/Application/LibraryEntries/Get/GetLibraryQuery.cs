using KitapKivo.Modules.Reading.Domain.LibraryEntries;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Get;

internal sealed record GetLibraryQuery(
    Guid UserId,
    LibraryEntryStatus? Status,
    int Page,
    int PageSize);