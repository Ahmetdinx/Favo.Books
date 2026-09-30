namespace Favo.Books.Modules.Identity
    .Application.Abstractions.Security;

public interface IRefreshTokenGenerator
{
    string Generate();
}