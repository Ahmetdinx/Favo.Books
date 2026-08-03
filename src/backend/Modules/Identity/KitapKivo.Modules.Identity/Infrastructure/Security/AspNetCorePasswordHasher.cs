using KitapKivo.Modules.Identity.Application.Abstractions.Security;

using Microsoft.AspNetCore.Identity;

namespace KitapKivo.Modules.Identity.Infrastructure.Security;

internal sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    private static readonly object HashingContext = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return _passwordHasher.HashPassword(
            HashingContext,
            password);
    }

    public bool Verify(
        string passwordHash,
        string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        PasswordVerificationResult result =
            _passwordHasher.VerifyHashedPassword(
                HashingContext,
                passwordHash,
                providedPassword);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}