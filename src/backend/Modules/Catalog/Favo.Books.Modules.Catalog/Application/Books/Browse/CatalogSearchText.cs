using System.Text;

namespace Favo.Books.Modules.Catalog.Application.Books.Browse;

internal static class CatalogSearchText
{
    internal const string TurkishLetters = "İIıŞşĞğÜüÖöÇçÂâÎîÛû";
    internal const string PlainLetters = "iiissgguuooccaaiiuu";

    private const int MaximumTerms = 8;

    public static string Fold(string source, string from, string to) =>
        throw new NotSupportedException("Yalnızca veritabanı sorgularında kullanılabilir.");

    public static string[] Terms(string query) =>
        query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Take(MaximumTerms)
            .ToArray();

    public static string Normalize(string value)
    {
        StringBuilder result = new(value.Length);

        foreach (char character in value.Normalize(NormalizationForm.FormC))
        {
            int index = TurkishLetters.IndexOf(character, StringComparison.Ordinal);
            result.Append(index >= 0 ? PlainLetters[index] : char.ToLowerInvariant(character));
        }

        return result.ToString();
    }

    public static string Contains(string term) => $"%{Escape(term)}%";

    public static string StartsWith(string term) => $"{Escape(term)}%";

    public static string IsbnDigits(string term) => term.Replace("-", string.Empty, StringComparison.Ordinal);

    private static string Escape(string term) => term
        .Replace(@"\", @"\\", StringComparison.Ordinal)
        .Replace("%", @"\%", StringComparison.Ordinal)
        .Replace("_", @"\_", StringComparison.Ordinal);
}
