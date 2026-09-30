using Favo.Books.Modules.Identity
    .Application.Abstractions.Security;
using Favo.Books.Modules.Identity
    .Domain.RefreshTokens;
using Favo.Books.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Favo.Books.Modules.Identity
    .Application.Users.LogoutSession;

public sealed class LogoutSessionHandler
{
    private readonly IdentityDbContext _dbContext;
    private readonly IRefreshTokenHasher _hasher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LogoutSessionHandler> _logger;

    public LogoutSessionHandler(
        IdentityDbContext dbContext,
        IRefreshTokenHasher hasher,
        TimeProvider timeProvider,
        ILogger<LogoutSessionHandler> logger)
    {
        _dbContext = dbContext;
        _hasher = hasher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task HandleAsync(
        LogoutSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(
                command.RefreshToken))
        {
            return;
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

            return;
        }

        DateTimeOffset revokedAtUtc =
            _timeProvider.GetUtcNow();

        if (currentToken.RevokedAtUtc is null)
        {
            currentToken.Revoke(revokedAtUtc);
        }
        else if (currentToken.ReplacedByTokenId.HasValue)
        {
            await RevokeTokenFamilyAsync(
                currentToken,
                revokedAtUtc,
                cancellationToken);

            _logger.LogWarning(
                "Logout received a rotated refresh token " +
                "for user {UserId}, family {FamilyId}.",
                currentToken.UserId,
                currentToken.FamilyId);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private async Task RevokeTokenFamilyAsync(
        RefreshToken token,
        DateTimeOffset revokedAtUtc,
        CancellationToken cancellationToken)
    {
        List<RefreshToken> unrevokedTokens =
            await _dbContext.RefreshTokens
                .Where(candidate =>
                    candidate.UserId == token.UserId &&
                    candidate.FamilyId == token.FamilyId &&
                    candidate.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

        foreach (RefreshToken unrevokedToken
                 in unrevokedTokens)
        {
            unrevokedToken.Revoke(revokedAtUtc);
        }
    }
}