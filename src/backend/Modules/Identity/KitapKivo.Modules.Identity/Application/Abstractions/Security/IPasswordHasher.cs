namespace KitapKivo.Modules.Identity.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);
}