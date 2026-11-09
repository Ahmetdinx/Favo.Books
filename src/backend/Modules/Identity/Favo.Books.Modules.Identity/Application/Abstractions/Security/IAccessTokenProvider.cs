namespace Favo.Books.Modules.Identity.Application.Abstractions.Security;

public interface IAccessTokenProvider
{
    AccessToken Generate(
        Guid userId,
        string email,
        bool isAdmin);
}