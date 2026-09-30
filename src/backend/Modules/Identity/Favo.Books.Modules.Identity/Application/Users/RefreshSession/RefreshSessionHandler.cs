using Favo.Books.Modules.Identity
    .Application.Abstractions.Security;
using Favo.Books.Modules.Identity
    .Domain.RefreshTokens;
using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Favo.Books.Modules.Identity
    .Application.Users.RefreshSession;

public sealed class RefreshSessionHandler
{
    private readonly IdentityDbContext _dbContext;
    private readonly IRefreshTokenHasher _hasher;
    private readonly IRefreshTokenIssuer _issuer;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RefreshSessionHandler> _logger;

    public RefreshSessionHandler(
        IdentityDbContext dbContext,
        IRefreshTokenHasher hasher,
        IRefreshTokenIssuer issuer,
        IAccessTokenProvider accessTokenProvider,
        TimeProvider timeProvider,
        ILogger<RefreshSessionHandler> logger)
    {
        _dbContext = dbContext;
        _hasher = hasher;
        _issuer = issuer;
        _accessTokenProvider = accessTokenProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<RefreshSessionResult> HandleAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(
                command.RefreshToken))
        {
            return RefreshSessionResult.InvalidToken();
        }

        string tokenHash =
            _hasher.Hash(command.RefreshToken);

        await using IDbContextTransaction transaction =
            await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

        List<RefreshToken> matchingTokens =
            await _dbContext.RefreshTokens
                .FromSqlInterpolated(
                    $"""
                    SELECT *
                    FROM identity.refresh_tokens
                    WHERE "TokenHash" = {tokenHash}
                    FOR UPDATE
                    """)
                .ToListAsync(cancellationToken);

        RefreshToken? currentToken =
            matchingTokens.SingleOrDefault();

        if (currentToken is null)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return RefreshSessionResult.InvalidToken();
        }

        DateTimeOffset nowUtc =
            _timeProvider.GetUtcNow();

        if (!currentToken.IsActive(nowUtc))
        {
            if (currentToken.RevokedAtUtc.HasValue &&
                currentToken.ReplacedByTokenId.HasValue)
            {
                await RevokeTokenFamilyAsync(
                    currentToken,
                    nowUtc,
                    cancellationToken);

                _logger.LogWarning(
                    "Refresh token reuse detected for " +
                    "user {UserId}, family {FamilyId}.",
                    currentToken.UserId,
                    currentToken.FamilyId);
            }

            await transaction.CommitAsync(
                cancellationToken);

            return RefreshSessionResult.InvalidToken();
        }

        User? user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == currentToken.UserId,
                cancellationToken);

        if (user is null)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return RefreshSessionResult.InvalidToken();
        }

        IssuedRefreshToken issuedRefreshToken =
            _issuer.Issue();

        RefreshToken replacementToken =
            currentToken.Rotate(
                issuedRefreshToken.Hash,
                issuedRefreshToken.CreatedAtUtc,
                issuedRefreshToken.ExpiresAtUtc);

        _dbContext.RefreshTokens.Add(
            replacementToken);

        AccessToken accessToken =
            _accessTokenProvider.Generate(
                user.Id,
                user.Email);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return RefreshSessionResult.Success(
            user.Id,
            user.Email,
            accessToken,
            issuedRefreshToken);
    }

    private async Task RevokeTokenFamilyAsync(
        RefreshToken reusedToken,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        List<RefreshToken> unrevokedTokens =
            await _dbContext.RefreshTokens
                .Where(token =>
                    token.UserId == reusedToken.UserId &&
                    token.FamilyId ==
                        reusedToken.FamilyId &&
                    token.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

        foreach (RefreshToken token in unrevokedTokens)
        {
            token.Revoke(revokedAtUtc);
        }

        if (unrevokedTokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }
}