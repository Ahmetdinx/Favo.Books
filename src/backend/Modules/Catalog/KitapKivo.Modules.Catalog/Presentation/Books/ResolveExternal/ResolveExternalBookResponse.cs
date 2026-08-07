namespace KitapKivo.Modules.Catalog.Presentation
    .Books.ResolveExternal;

internal sealed record ResolveExternalBookResponse(
    Guid BookId,
    bool IsCreated);