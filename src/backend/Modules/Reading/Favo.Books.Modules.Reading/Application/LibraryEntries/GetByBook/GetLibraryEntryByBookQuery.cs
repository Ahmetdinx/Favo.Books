namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.GetByBook;

internal sealed record GetLibraryEntryByBookQuery(
    Guid UserId,
    Guid BookId);