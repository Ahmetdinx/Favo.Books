namespace KitapKivo.Modules.Reading
    .Presentation.LibraryEntries.UpdateStatus;

public sealed record UpdateLibraryStatusResponse(
    Guid LibraryEntryId,
    string Status,
    DateTimeOffset UpdatedAtUtc);