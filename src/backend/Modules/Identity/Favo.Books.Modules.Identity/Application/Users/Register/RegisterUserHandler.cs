using Favo.Books.Modules.Identity.Application.Abstractions.Security;
using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Identity.Application.Users.Register;

public sealed class RegisterUserHandler
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        string normalizedEmail = command.Email
            .Trim()
            .ToLowerInvariant();

        bool emailAlreadyExists = await _dbContext.Users
            .AnyAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);

        if (emailAlreadyExists)
        {
            return RegisterUserResult.EmailAlreadyExists();
        }

        var nickname = UserNickname.Normalize(command.Nickname);
        if (await _dbContext.Users.AnyAsync(u => u.Nickname == nickname, cancellationToken))
            return new(null, RegisterUserError.NicknameAlreadyExists);

        string passwordHash =
            _passwordHasher.Hash(command.Password);

        User user = User.Create(
            normalizedEmail,
            passwordHash,
            DateTimeOffset.UtcNow);

        user.SetProfile(nickname, false);
        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" } pg)
        {
            return new(null, pg.ConstraintName == "IX_users_Nickname"
                ? RegisterUserError.NicknameAlreadyExists : RegisterUserError.EmailAlreadyExists);
        }

        return RegisterUserResult.Success(user.Id);
    }
}
