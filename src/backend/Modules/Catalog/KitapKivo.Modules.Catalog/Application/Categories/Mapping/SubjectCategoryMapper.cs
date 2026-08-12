using KitapKivo.Modules.Catalog.Domain.Categories;

namespace KitapKivo.Modules.Catalog.Application
    .Categories.Mapping;

internal static class SubjectCategoryMapper
{
    private static readonly CategoryRule[] Rules =
    [
        new(
            CatalogCategorySlugs.Novel,
            [
                "fiction",
                "novel",
                "romanlar"
            ]),
        new(
            CatalogCategorySlugs.Classics,
            [
                "classic literature",
                "literary classics",
                "classics",
                "klasikler"
            ]),
        new(
            CatalogCategorySlugs.ScienceFiction,
            [
                "science fiction",
                "dystopian",
                "space opera",
                "cyberpunk",
                "bilim kurgu"
            ]),
        new(
            CatalogCategorySlugs.Fantasy,
            [
                "fantasy",
                "fantastik"
            ]),
        new(
            CatalogCategorySlugs.CrimeAndMystery,
            [
                "detective",
                "mystery",
                "crime",
                "thriller",
                "polisiye",
                "gizem"
            ]),
        new(
            CatalogCategorySlugs.History,
            [
                "history",
                "historical",
                "tarih"
            ]),
        new(
            CatalogCategorySlugs.Philosophy,
            [
                "philosophy",
                "philosophical",
                "ethics",
                "existentialism",
                "felsefe"
            ]),
        new(
            CatalogCategorySlugs.Psychology,
            [
                "psychology",
                "psychological",
                "psychoanalysis",
                "mental health",
                "psikoloji"
            ]),
        new(
            CatalogCategorySlugs.Biography,
            [
                "biography",
                "autobiography",
                "memoir",
                "biyografi"
            ]),
        new(
            CatalogCategorySlugs.Poetry,
            [
                "poetry",
                "poems",
                "şiir"
            ]),
        new(
            CatalogCategorySlugs.Children,
            [
                "children",
                "juvenile",
                "çocuk"
            ]),
        new(
            CatalogCategorySlugs.PersonalDevelopment,
            [
                "self-help",
                "personal development",
                "motivational",
                "kişisel gelişim"
            ])
    ];

    public static IReadOnlyList<string> Map(
        IEnumerable<string> subjects)
    {
        ArgumentNullException.ThrowIfNull(subjects);

        string[] normalizedSubjects = subjects
            .Where(subject =>
                !string.IsNullOrWhiteSpace(subject))
            .Select(subject =>
                subject.Trim().ToLowerInvariant())
            .ToArray();

        if (normalizedSubjects.Length == 0)
        {
            return [];
        }

        return Rules
            .Where(rule =>
                normalizedSubjects.Any(subject =>
                    rule.Keywords.Any(keyword =>
                        subject.Contains(
                            keyword,
                            StringComparison.Ordinal))))
            .Select(rule => rule.CategorySlug)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(slug => slug)
            .ToArray();
    }

    private sealed record CategoryRule(
        string CategorySlug,
        IReadOnlyList<string> Keywords);
}