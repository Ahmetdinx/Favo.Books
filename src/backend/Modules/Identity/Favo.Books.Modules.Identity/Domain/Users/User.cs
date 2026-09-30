namespace Favo.Books.Modules.Identity.Domain.Users;

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

    public string? GoogleSubject { get; private set; }
    public void SetGoogleSubject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        if (subject.Length > 255 || GoogleSubject is not null) throw new ArgumentException("Invalid Google subject.");
        GoogleSubject = subject;
    }

    public string? Nickname { get; private set; }
    public bool IsPublic { get; private set; }
    public string AvatarId { get; private set; } = "owl";

    public static bool IsValidAvatar(string avatarId) =>
        avatarId is "owl" or "fox" or "cat" or "bear" or "deer" or "panda" or "penguin" or "rabbit";

    public void SetAvatar(string avatarId)
    {
        if (!IsValidAvatar(avatarId)) throw new ArgumentException("Geçerli bir avatar seçin.", nameof(avatarId));
        AvatarId = avatarId;
    }

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
