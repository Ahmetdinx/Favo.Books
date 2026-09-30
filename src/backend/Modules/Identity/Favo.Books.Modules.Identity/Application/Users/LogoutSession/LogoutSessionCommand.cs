namespace Favo.Books.Modules.Identity
    .Application.Users.LogoutSession;

public sealed record LogoutSessionCommand(
    string RefreshToken);