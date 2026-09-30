namespace Favo.Books.Modules.Catalog.Application.Books.GetById;

public sealed record GetBookByIdResult(
    Guid BookId,
    string Title,
    string OriginalLanguageCode,
    string? OriginalTitle,
    string? Description,
    int? FirstPublishedYear,
    Guid? PrimaryEditionId,
    string? PrimaryCoverImageUrl,
    IReadOnlyList<GetBookAuthorResult> Authors,
    IReadOnlyList<GetBookCategoryResult> Categories,
    IReadOnlyList<GetBookEditionResult> Editions);

public sealed record GetBookAuthorResult(
    Guid AuthorId,
    string Name,
    int DisplayOrder);

public sealed record GetBookCategoryResult(
    Guid CategoryId,
    string Name,
    string Slug);

public sealed record GetBookEditionResult(
    Guid EditionId,
    string Title,
    string LanguageCode,
    string Format,
    string? Isbn10,
    string? Isbn13,
    int? PageCount,
    int? PublicationYear,
    string? CoverImageUrl,
    GetBookPublisherResult? Publisher);

public sealed record GetBookPublisherResult(
    Guid PublisherId,
    string Name);