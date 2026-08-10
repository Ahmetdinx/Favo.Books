using KitapKivo.Modules.Reading.Domain.LibraryEntries;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.UpdateStatus;

internal sealed record UpdateLibraryStatusCommand(
    Guid UserId,
    Guid LibraryEntryId,
    LibraryEntryStatus Status);