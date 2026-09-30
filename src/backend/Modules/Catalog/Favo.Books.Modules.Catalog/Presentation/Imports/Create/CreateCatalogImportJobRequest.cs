namespace Favo.Books.Modules.Catalog.Presentation
    .Imports.Create;

internal sealed record CreateCatalogImportJobRequest(
    string? Provider,
    string? Query,
    int StartPage,
    int PageSize,
    int PageCount);