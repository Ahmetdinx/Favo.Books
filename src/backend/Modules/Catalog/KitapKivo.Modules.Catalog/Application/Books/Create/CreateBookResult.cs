namespace KitapKivo.Modules.Catalog.Application.Books.Create;

public sealed record CreateBookResult(
    Guid? BookId,
    IReadOnlyCollection<Guid> MissingAuthorIds)
{
    public bool IsSuccess => BookId.HasValue;

    public static CreateBookResult Success(
        Guid bookId)
    {
        return new CreateBookResult(
            bookId,
            Array.Empty<Guid>());
    }

    public static CreateBookResult AuthorsNotFound(
        IReadOnlyCollection<Guid> authorIds)
    {
        return new CreateBookResult(
            null,
            authorIds);
    }
}