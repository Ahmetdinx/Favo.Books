namespace Favo.Books.Modules.Identity.Application.Users.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string? Nickname = null);
