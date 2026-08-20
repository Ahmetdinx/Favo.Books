namespace KitapKivo.Modules.Catalog.Application
    .Books.GetSummaries;

public sealed record GetBookSummariesResult(
    IReadOnlyList<GetBookSummaryItemResult> Items);

public sealed record GetBookSummaryItemResult(
    Guid BookId,
    string Title,
    int? FirstPublishedYear,
    string? CoverImageUrl,
    IReadOnlyList<GetBookSummaryAuthorResult> Authors,
    IReadOnlyList<GetBookSummaryCategoryResult> Categories);

public sealed record GetBookSummaryAuthorResult(
    Guid AuthorId,
    string Name);

public sealed record GetBookSummaryCategoryResult(
    Guid CategoryId,
    string Name,
    string Slug);