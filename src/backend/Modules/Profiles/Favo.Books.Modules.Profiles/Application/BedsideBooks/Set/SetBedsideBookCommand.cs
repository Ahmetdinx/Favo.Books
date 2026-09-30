namespace Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Set;

internal sealed record SetBedsideBookCommand(
    Guid UserId,
    Guid BookId,
    string? PersonalNote);

internal sealed record SetBedsideBookResult(
    Guid BedsideBookId,
    Guid BookId,
    string? PersonalNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);