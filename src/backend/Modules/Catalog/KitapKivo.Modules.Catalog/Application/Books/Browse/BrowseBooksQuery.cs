namespace KitapKivo.Modules.Catalog.Application.Books.Browse;

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
    FirstPublishedNewest
}