namespace KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;

public sealed record ResolveExternalBookCommand(
    string Provider,
    string ExternalId);