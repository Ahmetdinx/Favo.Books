using System.Text;

using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;

namespace KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

internal static class OpenLibraryAuthorDeduplicator
{
    public static IReadOnlyList<ExternalBookAuthor>
        Deduplicate(
            IEnumerable<OpenLibraryResolvedAuthor> authors)
    {
        ArgumentNullException.ThrowIfNull(authors);

        OpenLibraryResolvedAuthor[] candidates =
            authors.ToArray();

        bool[] removed =
            new bool[candidates.Length];

        for (int leftIndex = 0;
             leftIndex < candidates.Length;
             leftIndex++)
        {
            if (removed[leftIndex])
            {
                continue;
            }

            for (int rightIndex = leftIndex + 1;
                 rightIndex < candidates.Length;
                 rightIndex++)
            {
                if (removed[rightIndex] ||
                    !AreSamePerson(
                        candidates[leftIndex],
                        candidates[rightIndex]))
                {
                    continue;
                }

                int leftScore =
                    GetQualityScore(candidates[leftIndex]);

                int rightScore =
                    GetQualityScore(candidates[rightIndex]);

                if (rightScore > leftScore)
                {
                    removed[leftIndex] = true;
                    break;
                }

                removed[rightIndex] = true;
            }
        }

        return candidates
            .Where((_, index) => !removed[index])
            .Select(author =>
                new ExternalBookAuthor(
                    author.ExternalId,
                    author.Name))
            .ToArray();
    }

    private static bool AreSamePerson(
        OpenLibraryResolvedAuthor left,
        OpenLibraryResolvedAuthor right)
    {
        return MatchesAlternativeName(
                   left.Name,
                   right.AlternativeNames) ||
               MatchesAlternativeName(
                   right.Name,
                   left.AlternativeNames);
    }

    private static bool MatchesAlternativeName(
        string name,
        IEnumerable<string> alternativeNames)
    {
        string normalizedName =
            NormalizeName(name);

        return alternativeNames
            .Where(alternativeName =>
                !string.IsNullOrWhiteSpace(
                    alternativeName))
            .Select(NormalizeName)
            .Any(alternativeName =>
                AreEquivalent(
                    normalizedName,
                    alternativeName));
    }

    private static bool AreEquivalent(
        string left,
        string right)
    {
        if (string.Equals(
                left,
                right,
                StringComparison.Ordinal))
        {
            return true;
        }

        if (Math.Min(left.Length, right.Length) < 8)
        {
            return false;
        }

        return IsSingleEditApart(left, right);
    }

    private static bool IsSingleEditApart(
        string left,
        string right)
    {
        if (Math.Abs(left.Length - right.Length) > 1)
        {
            return false;
        }

        int leftIndex = 0;
        int rightIndex = 0;
        int differences = 0;

        while (leftIndex < left.Length &&
               rightIndex < right.Length)
        {
            if (left[leftIndex] == right[rightIndex])
            {
                leftIndex++;
                rightIndex++;
                continue;
            }

            differences++;

            if (differences > 1)
            {
                return false;
            }

            if (left.Length > right.Length)
            {
                leftIndex++;
            }
            else if (right.Length > left.Length)
            {
                rightIndex++;
            }
            else
            {
                leftIndex++;
                rightIndex++;
            }
        }

        if (leftIndex < left.Length ||
            rightIndex < right.Length)
        {
            differences++;
        }

        return differences <= 1;
    }

    private static int GetQualityScore(
        OpenLibraryResolvedAuthor author)
    {
        return author.AlternativeNames.Count(name =>
            !string.IsNullOrWhiteSpace(name));
    }

    private static string NormalizeName(
        string name)
    {
        return new string(
            name.Normalize(NormalizationForm.FormKC)
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }
}