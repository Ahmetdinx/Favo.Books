using System.Security.Cryptography;

using KitapKivo.Modules.Identity
    .Application.Abstractions.Security;

namespace KitapKivo.Modules.Identity
    .Infrastructure.Authentication;

internal sealed class CryptographicRefreshTokenGenerator
    : IRefreshTokenGenerator
{
    private const int TokenByteLength = 64;

    public string Generate()
    {
        byte[] randomBytes =
            RandomNumberGenerator.GetBytes(
                TokenByteLength);

        try
        {
            return Convert
                .ToBase64String(randomBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        finally
        {
            CryptographicOperations.ZeroMemory(
                randomBytes);
        }
    }
}