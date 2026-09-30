namespace Favo.Books.Modules.Identity.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string providedPassword);
}