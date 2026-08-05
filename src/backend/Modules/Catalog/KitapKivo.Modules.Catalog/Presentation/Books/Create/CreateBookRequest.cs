namespace KitapKivo.Modules.Catalog.Presentation.Books.Create;

public sealed record CreateBookRequest(
    string? Title,
    string? OriginalLanguageCode,
    string? OriginalTitle,
    string? Description,
    int? FirstPublishedYear,
    Guid[]? AuthorIds);