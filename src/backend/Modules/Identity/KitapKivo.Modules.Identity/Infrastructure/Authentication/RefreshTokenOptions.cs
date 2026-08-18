namespace KitapKivo.Modules.Identity
    .Infrastructure.Authentication;

internal sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    public int ExpirationDays { get; init; }

    public string CookieName { get; init; } =
        string.Empty;
}