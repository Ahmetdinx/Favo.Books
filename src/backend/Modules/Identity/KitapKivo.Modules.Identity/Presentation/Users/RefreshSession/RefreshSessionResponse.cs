namespace KitapKivo.Modules.Identity
    .Presentation.Users.RefreshSession;

public sealed record RefreshSessionResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);