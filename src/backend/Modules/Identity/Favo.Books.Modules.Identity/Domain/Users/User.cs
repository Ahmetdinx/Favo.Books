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

    public bool IsAdmin { get; private set; }

    public string? Nickname { get; private set; }
    public bool IsPublic { get; private set; }
    public string AvatarId { get; private set; } = "owl";

    public static bool IsValidAvatar(string avatarId) =>
        avatarId is "owl" or "quill" or "lantern" or "moon" or "compass" or "key"
            or "hourglass" or "rose" or "book" or "tree" or "anchor" or "sun";

    public void SetAvatar(string avatarId)
    {
        if (!IsValidAvatar(avatarId)) throw new ArgumentException("Geçerli bir avatar seçin.", nameof(avatarId));
        AvatarId = avatarId;
    }

    /// <summary>The reader whose shared card, profile or link brought this account, and which kind of share it was.</summary>
    public Guid? ReferredById { get; private set; }
    public string? ReferralSource { get; private set; }

    public static bool IsValidReferralSource(string source) => source is "kart" or "link" or "uyum" or "kitap" or "profil";

    /// <summary>Recorded once, at sign-up; a reader cannot refer themselves and an unknown source is stored as "profil".</summary>
    public void SetReferral(Guid referrerId, string? source)
    {
        if (ReferredById is not null || referrerId == Id || referrerId == Guid.Empty) return;
        ReferredById = referrerId;
        ReferralSource = source is not null && IsValidReferralSource(source) ? source : "profil";
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
