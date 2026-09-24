namespace KitapKivo.Modules.Identity.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    private User(
        Guid id,
        string email,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string? Nickname { get; private set; }
    public bool IsPublic { get; private set; }

    public void SetProfile(string nickname, bool isPublic)
    {
        Nickname = UserNickname.Normalize(nickname);
        IsPublic = isPublic;
    }

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static User Create(string email,string passwordHash,DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        if (createdAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Oluşturulma zamanı UTC olmalıdır.",nameof(createdAtUtc));
        }

        return new User(Guid.NewGuid(),email.Trim().ToLowerInvariant(),passwordHash,createdAtUtc);
    }
}
