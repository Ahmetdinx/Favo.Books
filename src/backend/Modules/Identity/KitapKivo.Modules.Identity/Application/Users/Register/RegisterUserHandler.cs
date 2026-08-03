using KitapKivo.Modules.Identity.Application.Abstractions.Security;
using KitapKivo.Modules.Identity.Domain.Users;
using KitapKivo.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Identity.Application.Users.Register;

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

        string passwordHash =
            _passwordHasher.Hash(command.Password);

        User user = User.Create(
            normalizedEmail,
            passwordHash,
            DateTimeOffset.UtcNow);

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return RegisterUserResult.Success(user.Id);
    }
}