namespace KitapKivo.Modules.Profiles
    .Presentation.BedsideBooks.Set;

public sealed record SetBedsideBookRequest(
    Guid BookId,
    string? PersonalNote);