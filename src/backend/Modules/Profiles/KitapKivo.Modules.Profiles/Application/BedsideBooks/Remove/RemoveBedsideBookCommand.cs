namespace KitapKivo.Modules.Profiles
    .Application.BedsideBooks.Remove;

internal sealed record RemoveBedsideBookCommand(
    Guid UserId);

internal sealed record RemoveBedsideBookResult(
    bool IsSuccess)
{
    public static RemoveBedsideBookResult
        Success()
    {
        return new(
            IsSuccess: true);
    }

    public static RemoveBedsideBookResult
        NotFound()
    {
        return new(
            IsSuccess: false);
    }
}