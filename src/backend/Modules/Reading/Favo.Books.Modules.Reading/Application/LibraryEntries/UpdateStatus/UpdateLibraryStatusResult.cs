using Favo.Books.Modules.Reading.Domain.LibraryEntries;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.UpdateStatus;

internal enum UpdateLibraryStatusError
{
    None = 0,
    NotFound = 1
}

internal sealed record UpdateLibraryStatusResult(
    Guid? LibraryEntryId,
    LibraryEntryStatus? Status,
    DateTimeOffset? UpdatedAtUtc,
    UpdateLibraryStatusError Error)
{
    public bool IsSuccess =>
        Error == UpdateLibraryStatusError.None;

    public static UpdateLibraryStatusResult Success(
        Guid libraryEntryId,
        LibraryEntryStatus status,
        DateTimeOffset updatedAtUtc)
    {
        return new UpdateLibraryStatusResult(
            libraryEntryId,
            status,
            updatedAtUtc,
            UpdateLibraryStatusError.None);
    }

    public static UpdateLibraryStatusResult NotFound()
    {
        return new UpdateLibraryStatusResult(
            null,
            null,
            null,
            UpdateLibraryStatusError.NotFound);
    }
}