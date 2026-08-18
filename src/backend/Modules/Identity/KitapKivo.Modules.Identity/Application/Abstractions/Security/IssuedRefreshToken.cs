namespace KitapKivo.Modules.Identity
    .Application.Abstractions.Security;

public sealed record IssuedRefreshToken(
    string Value,
    string Hash,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);