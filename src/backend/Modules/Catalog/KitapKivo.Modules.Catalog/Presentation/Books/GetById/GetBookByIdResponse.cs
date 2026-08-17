namespace KitapKivo.Modules.Catalog.Presentation.Books.GetById;

public sealed record GetBookByIdResponse(
    Guid BookId,
    string Title,
    string OriginalLanguageCode,
    string? OriginalTitle,
    string? Description,
    int? FirstPublishedYear,
    IReadOnlyList<GetBookAuthorResponse> Authors,
    IReadOnlyList<GetBookEditionResponse> Editions);

public sealed record GetBookAuthorResponse(
    Guid AuthorId,
    string Name,
    int DisplayOrder);

public sealed record GetBookEditionResponse(
    Guid EditionId,
    string Title,
    string LanguageCode,
    string Format,
    string? Isbn10,
    string? Isbn13,
    int? PageCount,
    int? PublicationYear,
    string? CoverImageUrl,
    GetBookPublisherResponse? Publisher);

public sealed record GetBookPublisherResponse(
    Guid PublisherId,
    string Name);