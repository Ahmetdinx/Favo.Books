namespace Favo.Books.Modules.Catalog.Presentation
    .Imports.Create;

internal sealed record CreateCatalogImportJobResponse(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string StatusUrl);