namespace KitapKivo.Modules.Identity.Domain.RefreshTokens;

public sealed class RefreshToken
{
    private const int TokenHashLength = 64;

    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid FamilyId { get; private set; }

    public string TokenHash { get; private set; } =
        string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Kullanıcı kimliği boş olamaz.",
                nameof(userId));
        }

        string normalizedTokenHash =
            ValidateTokenData(
                tokenHash,
                createdAtUtc,
                expiresAtUtc);

        return new RefreshToken(
            Guid.NewGuid(),
            userId,
            Guid.NewGuid(),
            normalizedTokenHash,
            createdAtUtc,
            expiresAtUtc);
    }

    public bool IsActive(DateTimeOffset nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));

        return RevokedAtUtc is null &&
               nowUtc < ExpiresAtUtc;
    }

    public RefreshToken Rotate(
        string replacementTokenHash,
        DateTimeOffset rotatedAtUtc,
        DateTimeOffset replacementExpiresAtUtc)
    {
        EnsureUtc(rotatedAtUtc, nameof(rotatedAtUtc));

        if (rotatedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotatedAtUtc));
        }

        if (!IsActive(rotatedAtUtc))
        {
            throw new InvalidOperationException(
                "Aktif olmayan refresh token yenilenemez.");
        }

        string normalizedTokenHash =
            ValidateTokenData(
                replacementTokenHash,
                rotatedAtUtc,
                replacementExpiresAtUtc);

        RefreshToken replacement =
            new(
                Guid.NewGuid(),
                UserId,
                FamilyId,
                normalizedTokenHash,
                rotatedAtUtc,
                replacementExpiresAtUtc);

        RevokedAtUtc = rotatedAtUtc;
        ReplacedByTokenId = replacement.Id;

        return replacement;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        EnsureUtc(revokedAtUtc, nameof(revokedAtUtc));

        if (revokedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revokedAtUtc));
        }

        if (RevokedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "Refresh token zaten iptal edilmiş.");
        }

        RevokedAtUtc = revokedAtUtc;
    }

    private static string ValidateTokenData(
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        string normalizedTokenHash = tokenHash.Trim();

        if (normalizedTokenHash.Length != TokenHashLength)
        {
            throw new ArgumentException(
                "Refresh token hash değeri 64 karakter olmalıdır.",
                nameof(tokenHash));
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));

        if (expiresAtUtc <= createdAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAtUtc),
                "Bitiş zamanı oluşturulma zamanından sonra olmalıdır.");
        }

        return normalizedTokenHash;
    }

    private static void EnsureUtc(
        DateTimeOffset value,
        string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Tarih UTC olmalıdır.",
                parameterName);
        }
    }
}