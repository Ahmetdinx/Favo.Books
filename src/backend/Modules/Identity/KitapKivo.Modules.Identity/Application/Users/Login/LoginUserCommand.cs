namespace KitapKivo.Modules.Identity.Application.Users.Login;

public sealed record LoginUserCommand(
    string Email,
    string Password);