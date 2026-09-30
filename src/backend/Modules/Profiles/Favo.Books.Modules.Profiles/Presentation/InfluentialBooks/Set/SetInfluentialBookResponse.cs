namespace Favo.Books.Modules.Profiles
    .Presentation.InfluentialBooks.Set;

public sealed record
    SetInfluentialBookResponse(
        Guid InfluentialBookId,
        Guid BookId,
        int Position,
        string? PersonalReflection,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);