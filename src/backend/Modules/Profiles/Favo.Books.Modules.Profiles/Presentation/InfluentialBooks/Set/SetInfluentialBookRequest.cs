namespace Favo.Books.Modules.Profiles
    .Presentation.InfluentialBooks.Set;

public sealed record
    SetInfluentialBookRequest(
        Guid BookId,
        string? PersonalReflection);