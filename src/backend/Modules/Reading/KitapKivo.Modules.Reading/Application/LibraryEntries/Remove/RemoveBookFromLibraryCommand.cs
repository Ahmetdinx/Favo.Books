namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Remove;

internal sealed record RemoveBookFromLibraryCommand(
    Guid UserId,
    Guid LibraryEntryId);