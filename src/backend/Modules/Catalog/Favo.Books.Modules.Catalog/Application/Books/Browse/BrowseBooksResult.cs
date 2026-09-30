namespace Favo.Books.Modules.Catalog.Application.Books.Browse;

public sealed record BrowseBooksResult(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<BrowseBookItemResult> Items);

public sealed record BrowseBookItemResult(
    Guid BookId,
    string Title,
    int? FirstPublishedYear,
    string? CoverImageUrl,
    IReadOnlyList<BrowseBookAuthorResult> Authors,
    IReadOnlyList<BrowseBookCategoryResult> Categories);

public sealed record BrowseBookAuthorResult(
    Guid AuthorId,
    string Name);

public sealed record BrowseBookCategoryResult(
    Guid CategoryId,
    string Name,
    string Slug);