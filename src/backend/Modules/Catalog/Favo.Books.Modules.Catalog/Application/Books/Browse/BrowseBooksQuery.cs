namespace Favo.Books.Modules.Catalog.Application.Books.Browse;

public sealed record BrowseBooksQuery(
    string? Query,
    CatalogBookSearchField SearchField,
    string? CategorySlug,
    int Page,
    int PageSize,
    CatalogBookSort Sort);

public enum CatalogBookSearchField
{
    All,
    Title,
    Author
}

public enum CatalogBookSort
{
    RecentlyAdded,
    Title,
    FirstPublishedNewest,
    // Curated lists were imported in rank order (well-known classics, then bestseller sales
    // position), so their import order is the best popularity signal until readers produce one.
    Popular
}