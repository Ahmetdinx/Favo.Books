namespace KitapKivo.Modules.Profiles
    .Application.InfluentialBooks.Get;

internal sealed record
    GetInfluentialBooksQuery(
        Guid UserId);

internal sealed record
    GetInfluentialBooksResult(
        IReadOnlyList<
            GetInfluentialBookItemResult>
                Items);

internal sealed record
    GetInfluentialBookItemResult(
        Guid InfluentialBookId,
        Guid BookId,
        int Position,
        string? PersonalReflection,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);