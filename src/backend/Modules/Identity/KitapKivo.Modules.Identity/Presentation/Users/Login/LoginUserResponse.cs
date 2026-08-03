namespace KitapKivo.Modules.Identity.Presentation.Users.Login;

public sealed record LoginUserResponse(Guid UserId,string Email,string AccessToken,DateTimeOffset ExpiresAtUtc);