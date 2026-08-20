namespace KitapKivo.Modules.Profiles
    .Presentation.BedsideBooks.Set;

public sealed record SetBedsideBookResponse(
    Guid BedsideBookId,
    Guid BookId,
    string? PersonalNote,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);