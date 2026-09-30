namespace Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.GetByBook;

public sealed record GetLibraryEntryByBookResponse(
    Guid LibraryEntryId,
    Guid BookId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);