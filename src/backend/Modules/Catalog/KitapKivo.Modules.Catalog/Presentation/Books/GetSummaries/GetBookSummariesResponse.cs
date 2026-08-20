namespace KitapKivo.Modules.Catalog.Presentation
    .Books.GetSummaries;

public sealed record GetBookSummariesResponse(
    IReadOnlyList<GetBookSummaryItemResponse> Items);

public sealed record GetBookSummaryItemResponse(
    Guid BookId,
    string Title,
    int? FirstPublishedYear,
    string? CoverImageUrl,
    IReadOnlyList<GetBookSummaryAuthorResponse> Authors,
    IReadOnlyList<GetBookSummaryCategoryResponse> Categories);

public sealed record GetBookSummaryAuthorResponse(
    Guid AuthorId,
    string Name);

public sealed record GetBookSummaryCategoryResponse(
    Guid CategoryId,
    string Name,
    string Slug);