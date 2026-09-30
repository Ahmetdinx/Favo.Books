namespace Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Get;

internal sealed record GetBedsideBookQuery(
    Guid UserId);

internal sealed record GetBedsideBookResult(
    Guid BedsideBookId,
    Guid BookId,
    string? PersonalNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);