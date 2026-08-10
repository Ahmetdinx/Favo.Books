using KitapKivo.Modules.Reading.Domain.LibraryEntries;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Add;

internal sealed record AddBookToLibraryCommand(
    Guid UserId,
    Guid BookId,
    LibraryEntryStatus Status);