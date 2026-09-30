using Favo.Books.Modules.Reading.Domain.LibraryEntries;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.Add;

internal sealed record AddBookToLibraryCommand(
    Guid UserId,
    Guid BookId,
    LibraryEntryStatus Status);