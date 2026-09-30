namespace Favo.Books.Modules.Profiles
    .Presentation.InfluentialBooks.Get;

public sealed record
    GetInfluentialBooksResponse(
        IReadOnlyList<
            GetInfluentialBookItemResponse>
                Items);

public sealed record
    GetInfluentialBookItemResponse(
        Guid InfluentialBookId,
        Guid BookId,
        int Position,
        string? PersonalReflection,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);