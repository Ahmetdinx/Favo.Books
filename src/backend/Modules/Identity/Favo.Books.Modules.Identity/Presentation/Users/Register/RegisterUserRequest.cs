namespace Favo.Books.Modules.Identity.Presentation.Users.Register;

public sealed record RegisterUserRequest(
    string? Email,
    string? Password,
    string? Nickname = null,
    string? Referrer = null,
    string? ReferralSource = null);
