namespace KitapKivo.Modules.Profiles
    .Application.InfluentialBooks.Set;

internal sealed record
    SetInfluentialBookCommand(
        Guid UserId,
        Guid BookId,
        int Position,
        string? PersonalReflection);

internal sealed record
    SetInfluentialBookResult(
        bool IsSuccess,
        Guid? InfluentialBookId,
        Guid? BookId,
        int? Position,
        string? PersonalReflection,
        DateTimeOffset? CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc)
{
    public static SetInfluentialBookResult
        Success(
            Guid influentialBookId,
            Guid bookId,
            int position,
            string? personalReflection,
            DateTimeOffset createdAtUtc,
            DateTimeOffset updatedAtUtc)
    {
        return new(
            true,
            influentialBookId,
            bookId,
            position,
            personalReflection,
            createdAtUtc,
            updatedAtUtc);
    }

    public static SetInfluentialBookResult
        DuplicateBook()
    {
        return new(
            false,
            null,
            null,
            null,
            null,
            null,
            null);
    }
}