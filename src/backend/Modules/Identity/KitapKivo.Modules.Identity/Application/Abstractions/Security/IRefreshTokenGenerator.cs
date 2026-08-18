namespace KitapKivo.Modules.Identity
    .Application.Abstractions.Security;

public interface IRefreshTokenGenerator
{
    string Generate();
}