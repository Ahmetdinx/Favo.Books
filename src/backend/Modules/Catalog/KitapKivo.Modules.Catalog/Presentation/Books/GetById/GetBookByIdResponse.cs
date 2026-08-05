namespace KitapKivo.Modules.Catalog.Presentation.Books.GetById;

public sealed record GetBookByIdResponse(
    Guid BookId,
    string Title,
    string OriginalLanguageCode,
    string? OriginalTitle,
    string? Description,
    int? FirstPublishedYear,
    IReadOnlyList<GetBookAuthorResponse> Authors);

public sealed record GetBookAuthorResponse(
    Guid AuthorId,
    string Name,
    int DisplayOrder);