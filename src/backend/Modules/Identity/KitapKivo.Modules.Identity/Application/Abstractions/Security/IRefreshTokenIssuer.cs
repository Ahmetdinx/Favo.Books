namespace KitapKivo.Modules.Identity
    .Application.Abstractions.Security;

public interface IRefreshTokenIssuer
{
    IssuedRefreshToken Issue();
}