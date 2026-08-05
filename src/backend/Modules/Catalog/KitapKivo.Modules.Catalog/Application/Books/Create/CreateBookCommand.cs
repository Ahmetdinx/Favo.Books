namespace KitapKivo.Modules.Catalog.Application.Books.Create;

public sealed record CreateBookCommand(
    string Title,
    string OriginalLanguageCode,
    string? OriginalTitle,
    string? Description,
    int? FirstPublishedYear,
    IReadOnlyList<Guid> AuthorIds);