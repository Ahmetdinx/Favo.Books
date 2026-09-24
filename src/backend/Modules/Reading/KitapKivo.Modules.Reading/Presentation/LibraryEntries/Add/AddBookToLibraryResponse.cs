namespace KitapKivo.Modules.Reading
    .Presentation.LibraryEntries.Add;

public sealed record AddBookToLibraryResponse(
    Guid LibraryEntryId,
    DateTimeOffset UpdatedAtUtc);
