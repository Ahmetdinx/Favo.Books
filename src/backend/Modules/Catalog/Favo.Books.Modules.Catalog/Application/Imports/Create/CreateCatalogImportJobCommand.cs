namespace Favo.Books.Modules.Catalog.Application
    .Imports.Create;

public sealed record CreateCatalogImportJobCommand(
    string Provider,
    string Query,
    int StartPage,
    int PageSize,
    int PageCount);