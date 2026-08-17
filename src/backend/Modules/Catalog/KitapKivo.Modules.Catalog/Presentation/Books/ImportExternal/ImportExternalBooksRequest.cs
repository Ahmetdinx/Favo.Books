namespace KitapKivo.Modules.Catalog.Presentation
    .Books.ImportExternal;

internal sealed record ImportExternalBooksRequest(
    string? Provider,
    string? Query,
    int Page = 1,
    int PageSize = 10);