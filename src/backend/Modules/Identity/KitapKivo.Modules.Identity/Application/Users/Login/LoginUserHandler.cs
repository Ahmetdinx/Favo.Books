using KitapKivo.Modules.Identity.Application.Abstractions.Security;
using KitapKivo.Modules.Identity.Domain.Users;
using KitapKivo.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Identity.Application.Users.Login;

public sealed class LoginUserHandler
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenProvider _accessTokenProvider;

    public LoginUserHandler(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAccessTokenProvider accessTokenProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _accessTokenProvider = accessTokenProvider;
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

        return LoginUserResult.Success(
            user.Id,
            user.Email,
            accessToken);
    }
}