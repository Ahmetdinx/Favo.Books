using System.Security.Cryptography;

using Favo.Books.Modules.Identity
    .Application.Abstractions.Security;

namespace Favo.Books.Modules.Identity
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