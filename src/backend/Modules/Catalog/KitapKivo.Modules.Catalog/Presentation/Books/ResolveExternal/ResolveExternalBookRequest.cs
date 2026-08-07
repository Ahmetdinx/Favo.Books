namespace KitapKivo.Modules.Catalog.Presentation
    .Books.ResolveExternal;

internal sealed record ResolveExternalBookRequest(
    string? Provider,
    string? ExternalId);