using Favo.Books.Modules.Identity
    .Application.Abstractions.Security;

using Microsoft.Extensions.Options;

namespace Favo.Books.Modules.Identity
    .Infrastructure.Authentication;

internal sealed class RefreshTokenIssuer
    : IRefreshTokenIssuer
{
    private readonly IRefreshTokenGenerator _generator;
    private readonly IRefreshTokenHasher _hasher;
    private readonly RefreshTokenOptions _options;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenIssuer(
        IRefreshTokenGenerator generator,
        IRefreshTokenHasher hasher,
        IOptions<RefreshTokenOptions> options,
        TimeProvider timeProvider)
    {
        _generator = generator;
        _hasher = hasher;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public IssuedRefreshToken Issue()
    {
        DateTimeOffset createdAtUtc =
            _timeProvider.GetUtcNow();

        string value = _generator.Generate();
        string hash = _hasher.Hash(value);

        DateTimeOffset expiresAtUtc =
            createdAtUtc.AddDays(
                _options.ExpirationDays);

        return new IssuedRefreshToken(
            value,
            hash,
            createdAtUtc,
            expiresAtUtc);
    }
}