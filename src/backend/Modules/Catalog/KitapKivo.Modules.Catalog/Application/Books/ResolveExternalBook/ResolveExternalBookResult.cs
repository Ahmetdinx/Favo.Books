namespace KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;

public sealed record ResolveExternalBookResult(
    Guid? BookId,
    bool IsCreated,
    string? RejectionReason = null)
{
    public bool IsSuccess => BookId.HasValue;
    public static ResolveExternalBookResult Rejected(string reason) => new(null, false, reason);

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
