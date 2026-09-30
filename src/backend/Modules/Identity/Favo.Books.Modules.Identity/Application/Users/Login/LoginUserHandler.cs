using Favo.Books.Modules.Identity.Application.Abstractions.Security;
using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Persistence;
using Favo.Books.Modules.Identity.Domain.RefreshTokens;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Identity.Application.Users.Login;

public sealed class LoginUserHandler
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly IRefreshTokenIssuer _refreshTokenIssuer;

    public LoginUserHandler(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAccessTokenProvider accessTokenProvider,
        IRefreshTokenIssuer refreshTokenIssuer)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _accessTokenProvider = accessTokenProvider;
        _refreshTokenIssuer = refreshTokenIssuer;
    }

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string normalizedEmail = command.Email
            .Trim()
            .ToLowerInvariant();

        User? user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);

        if (user is null)
        {
            return LoginUserResult.InvalidCredentials();
        }

        bool passwordIsValid = _passwordHasher.Verify(
            user.PasswordHash,
            command.Password);

        if (!passwordIsValid)
        {
            return LoginUserResult.InvalidCredentials();
        }

        AccessToken accessToken =
            _accessTokenProvider.Generate(
                user.Id,
                user.Email);


        IssuedRefreshToken issuedRefreshToken =
       _refreshTokenIssuer.Issue();

        RefreshToken refreshToken =
            RefreshToken.Create(
                user.Id,
                issuedRefreshToken.Hash,
                issuedRefreshToken.CreatedAtUtc,
                issuedRefreshToken.ExpiresAtUtc);

        _dbContext.RefreshTokens.Add(refreshToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return LoginUserResult.Success(
            user.Id,
            user.Email,
            accessToken,
            issuedRefreshToken);
    }
}