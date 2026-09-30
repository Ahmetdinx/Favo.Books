namespace Favo.Books.Modules.Identity
    .Application.Users.RefreshSession;

public sealed record RefreshSessionCommand(
    string RefreshToken);