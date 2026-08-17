namespace KitapKivo.Modules.Catalog.Application
    .Books.ImportExternal;

public sealed record ImportExternalBooksCommand(
    string Provider,
    string Query,
    int Page = 1,
    int PageSize = 10);