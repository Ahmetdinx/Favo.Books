namespace KitapKivo.Modules.Catalog.Application.Books.GetById;

public sealed record GetBookByIdResult(
    Guid BookId,
    string Title,
    string OriginalLanguageCode,
    string? OriginalTitle,
    string? Description,
    int? FirstPublishedYear,
    IReadOnlyList<GetBookAuthorResult> Authors);

public sealed record GetBookAuthorResult(
    Guid AuthorId,
    string Name,
    int DisplayOrder);