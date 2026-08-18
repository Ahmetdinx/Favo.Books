using System.Security.Cryptography;
using System.Text;

using KitapKivo.Modules.Identity
    .Application.Abstractions.Security;

namespace KitapKivo.Modules.Identity
    .Infrastructure.Authentication;

internal sealed class Sha256RefreshTokenHasher
    : IRefreshTokenHasher
{
    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            token);

        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(token);

        try
        {
            byte[] hashBytes =
                SHA256.HashData(tokenBytes);

            return Convert.ToHexString(hashBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                tokenBytes);
        }
    }
}