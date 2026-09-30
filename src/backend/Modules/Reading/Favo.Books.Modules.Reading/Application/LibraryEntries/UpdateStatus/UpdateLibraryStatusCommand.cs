using Favo.Books.Modules.Reading.Domain.LibraryEntries;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.UpdateStatus;

internal sealed record UpdateLibraryStatusCommand(
    Guid UserId,
    Guid LibraryEntryId,
    LibraryEntryStatus Status);