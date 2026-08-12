namespace KitapKivo.Modules.Catalog.Presentation.Books.Browse;

public sealed record BrowseBooksResponse(
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<BrowseBookItemResponse> Items);

public sealed record BrowseBookItemResponse(
    Guid BookId,
    string Title,
    int? FirstPublishedYear,
    string? CoverImageUrl,
    IReadOnlyList<BrowseBookAuthorResponse> Authors,
    IReadOnlyList<BrowseBookCategoryResponse> Categories);

public sealed record BrowseBookAuthorResponse(
    Guid AuthorId,
    string Name);

public sealed record BrowseBookCategoryResponse(
    Guid CategoryId,
    string Name,
    string Slug);