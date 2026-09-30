namespace Favo.Books.Modules.Catalog.Application
    .Imports.Create;

public sealed record CreateCatalogImportJobResult(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAtUtc);