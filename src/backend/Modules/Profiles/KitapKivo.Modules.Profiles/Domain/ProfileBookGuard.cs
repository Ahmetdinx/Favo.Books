namespace KitapKivo.Modules.Profiles.Domain;

internal static class ProfileBookGuard
{
    public static void EnsureIdentifierIsNotEmpty(
        Guid value,
        string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Kimlik boş olamaz.",
                parameterName);
        }
    }

    public static string? NormalizeOptionalText(
        string? value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Metin en fazla {maximumLength} " +
                "karakter olabilir.",
                parameterName);
        }

        return normalizedValue;
    }

    public static void EnsurePositionIsInRange(
        int position,
        int minimumPosition,
        int maximumPosition,
        string parameterName)
    {
        if (position < minimumPosition ||
            position > maximumPosition)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Sıra {minimumPosition} ile " +
                $"{maximumPosition} arasında olmalıdır.");
        }
    }

    public static void EnsureUtc(
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

    public static void EnsureUpdateDoesNotPrecedeCreation(
        DateTimeOffset updatedAtUtc,
        DateTimeOffset createdAtUtc,
        string parameterName)
    {
        if (updatedAtUtc < createdAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Güncellenme zamanı oluşturulma " +
                "zamanından önce olamaz.");
        }
    }
}