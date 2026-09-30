namespace Favo.Books.Modules.Profiles
    .Presentation.BedsideBooks.Get;

public sealed record GetBedsideBookResponse(
    Guid BedsideBookId,
    Guid BookId,
    string? PersonalNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);