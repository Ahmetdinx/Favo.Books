namespace Favo.Books.Modules.Profiles
    .Presentation.BedsideBooks.Set;

public sealed record SetBedsideBookRequest(
    Guid BookId,
    string? PersonalNote);