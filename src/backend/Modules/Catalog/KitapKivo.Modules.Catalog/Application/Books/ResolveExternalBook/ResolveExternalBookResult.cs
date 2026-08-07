namespace KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;

public sealed record ResolveExternalBookResult(
    Guid? BookId,
    bool IsCreated)
{
    public bool IsSuccess => BookId.HasValue;

    public static ResolveExternalBookResult Created(
        Guid bookId)
    {
        return new ResolveExternalBookResult(
            bookId,
            true);
    }

    public static ResolveExternalBookResult Existing(
        Guid bookId)
    {
        return new ResolveExternalBookResult(
            bookId,
            false);
    }

    public static ResolveExternalBookResult NotFound()
    {
        return new ResolveExternalBookResult(
            null,
            false);
    }
}