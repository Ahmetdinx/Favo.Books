namespace KitapKivo.Modules.Profiles
    .Application.InfluentialBooks.Remove;

internal sealed record
    RemoveInfluentialBookCommand(
        Guid UserId,
        int Position);

internal sealed record
    RemoveInfluentialBookResult(
        bool IsSuccess)
{
    public static RemoveInfluentialBookResult
        Success()
    {
        return new(true);
    }

    public static RemoveInfluentialBookResult
        NotFound()
    {
        return new(false);
    }
}