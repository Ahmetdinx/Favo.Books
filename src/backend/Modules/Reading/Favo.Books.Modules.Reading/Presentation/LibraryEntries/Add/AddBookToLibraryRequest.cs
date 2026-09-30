namespace Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.Add;

public sealed record AddBookToLibraryRequest(
    Guid BookId,
    string? Status);