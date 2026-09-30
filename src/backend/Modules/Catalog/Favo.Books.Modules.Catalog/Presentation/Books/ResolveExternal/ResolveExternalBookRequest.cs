namespace Favo.Books.Modules.Catalog.Presentation
    .Books.ResolveExternal;

internal sealed record ResolveExternalBookRequest(
    string? Provider,
    string? ExternalId);