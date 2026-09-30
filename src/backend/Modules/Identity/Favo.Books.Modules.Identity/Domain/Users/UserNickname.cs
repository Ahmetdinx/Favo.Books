using System.Text.RegularExpressions;

namespace Favo.Books.Modules.Identity.Domain.Users;

public static class UserNickname
{
    public const string Message = "Kullanıcı adı 3–24 karakter olmalı, bir harfle başlamalı ve yalnız a-z, rakam, alt çizgi içermelidir. Sistem adları kullanılamaz.";
    private static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
        { "admin", "administrator", "support", "destek", "moderator", "kitapkivo", "favo", "system", "api" };

    public static string Normalize(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? "";
        if (!Regex.IsMatch(normalized, "^[a-z][a-z0-9_]{2,23}$", RegexOptions.CultureInvariant) || Reserved.Contains(normalized))
            throw new ArgumentException(Message);
        return normalized;
    }
}
