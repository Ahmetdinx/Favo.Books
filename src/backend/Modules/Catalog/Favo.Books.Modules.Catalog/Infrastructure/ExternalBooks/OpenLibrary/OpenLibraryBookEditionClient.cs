using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary.Contracts;

namespace Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

internal sealed class OpenLibraryBookEditionClient(
    HttpClient httpClient)
    : IExternalBookEditionClient
{
    private const string ProviderName =
        "OpenLibrary";

    private const int EditionPageSize = 100;

    private static readonly Regex WorkIdPattern = new(
        @"^/?works/(OL\d+W)$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    private static readonly Regex EditionIdPattern = new(
        @"^/?books/(OL\d+M)$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    private static readonly Regex LanguageKeyPattern = new(
        @"^/?languages/([a-z]{2,3})$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    private static readonly Regex Isbn10Pattern = new(
        @"^\d{9}[\dX]$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    private static readonly Regex Isbn13Pattern = new(
        @"^\d{13}$",
        RegexOptions.CultureInvariant);

    private static readonly Regex YearPattern = new(
        @"(?<!\d)\d{4}(?!\d)",
        RegexOptions.CultureInvariant);

    private readonly HttpClient _httpClient =
        httpClient;

    public async Task<ExternalBookEditionDetails?>
        GetBestForWorkAsync(
            string externalWorkId,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExternalBookEditionDetails>
            editions =
                await GetBestEditionsForWorkAsync(
                    externalWorkId,
                    cancellationToken);

        return editions.FirstOrDefault();
    }

    public async Task<
        IReadOnlyList<ExternalBookEditionDetails>>
        GetBestEditionsForWorkAsync(
            string externalWorkId,
            CancellationToken cancellationToken = default)
    {
        string workId =
            NormalizeWorkId(externalWorkId);

        List<OpenLibraryEditionCandidate>
            turkishCandidates = [];

        int offset = 0;

        while (true)
        {
            string requestUri =
                CreateEditionsRequestUri(
                    workId,
                    offset);

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    requestUri,
                    cancellationToken);

            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                return [];
            }

            response.EnsureSuccessStatusCode();

            OpenLibraryWorkEditionsResponse?
                editionsResponse =
                    await response.Content
                        .ReadFromJsonAsync<
                            OpenLibraryWorkEditionsResponse>(
                            cancellationToken);

            if (editionsResponse is null)
            {
                throw new JsonException(
                    "Open Library returned an empty " +
                    "work editions response.");
            }

            turkishCandidates.AddRange(
                editionsResponse.Entries
                    .Select(CreateCandidate)
                    .OfType<
                        OpenLibraryEditionCandidate>());

            if (editionsResponse.Entries.Length == 0)
            {
                break;
            }

            offset += EditionPageSize;

            if (offset >= editionsResponse.Size)
            {
                break;
            }
        }

        OpenLibraryEditionCandidate? selectedCandidate =
            SelectBestCandidate(
                turkishCandidates);

        return selectedCandidate is null
            ? []
            : [MapCandidate(selectedCandidate)];
    }

    private static string CreateEditionsRequestUri(
        string workId,
        int offset)
    {
        return
            $"{workId.TrimStart('/')}/editions.json" +
            $"?limit={EditionPageSize}" +
            $"&offset={offset}";
    }

    private static OpenLibraryEditionCandidate?
        CreateCandidate(
            OpenLibraryEditionResponse edition)
    {
        string? externalId =
            TryNormalizeEditionId(edition.Key);

        string? languageCode =
            NormalizeLanguageCode(
                MapLanguageCode(
                    edition.Languages));

        string? title =
            NormalizeOptionalValue(
                edition.Title);

        if (externalId is null ||
            languageCode is null ||
            !IsTurkishLanguage(languageCode) ||
            title is null)
        {
            return null;
        }

        return new OpenLibraryEditionCandidate(
            edition,
            externalId,
            languageCode!);
    }

    private static OpenLibraryEditionCandidate?
        SelectBestCandidate(
            IEnumerable<
                OpenLibraryEditionCandidate>
                candidates)
    {
        return candidates
            .OrderBy(candidate =>
                GetPublisherPriority(
                    candidate.Edition.Publishers))
            .ThenByDescending(candidate =>
                MapCoverUrl(
                    candidate.Edition.CoverIds)
                is not null)
            .ThenByDescending(candidate =>
                FindIsbn13(
                    candidate.Edition.Isbn13Values)
                is not null)
            .ThenByDescending(candidate =>
                FindIsbn10(
                    candidate.Edition.Isbn10Values)
                is not null)
            .ThenByDescending(candidate =>
                candidate.Edition.NumberOfPages is > 0)
            .ThenByDescending(candidate =>
                NormalizeOptionalValue(
                    candidate.Edition.PhysicalFormat)
                is not null)
            .ThenByDescending(candidate =>
                MapPublicationYear(
                    candidate.Edition.PublishDate))
            .ThenBy(candidate =>
                candidate.ExternalId)
            .FirstOrDefault();
    }

    private static ExternalBookEditionDetails
        MapCandidate(
            OpenLibraryEditionCandidate candidate)
    {
        OpenLibraryEditionResponse edition =
            candidate.Edition;

        return new ExternalBookEditionDetails(
            ProviderName,
            candidate.ExternalId,
            NormalizeOptionalValue(
                edition.Title),
            candidate.LanguageCode,
            NormalizeOptionalValue(
                edition.PhysicalFormat),
            FindIsbn10(
                edition.Isbn10Values),
            FindIsbn13(
                edition.Isbn13Values),
            edition.NumberOfPages is > 0
                ? edition.NumberOfPages
                : null,
            MapPublicationYear(
                edition.PublishDate),
            MapCoverUrl(
                edition.CoverIds),
            FindPublisherName(
                edition.Publishers));
    }

    private static int GetPublisherPriority(
        IEnumerable<string> publisherNames)
    {
        int bestPriority = int.MaxValue;

        foreach (string publisherName
                 in publisherNames)
        {
            string? normalizedName =
                NormalizeOptionalValue(
                    publisherName);

            if (normalizedName is null)
            {
                continue;
            }

            int priority =
                GetPublisherPriority(
                    normalizedName);

            if (priority < bestPriority)
            {
                bestPriority = priority;
            }
        }

        return bestPriority;
    }

    private static int GetPublisherPriority(
        string publisherName)
    {
        string key =
            NormalizePublisherKey(
                publisherName);

        if (key.Contains(
                "BANKA",
                StringComparison.Ordinal) &&
            key.Contains(
                "KULTUR",
                StringComparison.Ordinal))
        {
            return 0;
        }

        if (string.Equals(
                key,
                "YKY",
                StringComparison.Ordinal) ||
            key.Contains(
                "YAPIKREDI",
                StringComparison.Ordinal))
        {
            return 1;
        }

        if (key.Contains(
                "METIS",
                StringComparison.Ordinal))
        {
            return 2;
        }

        if (key.Contains(
                "ILETISIM",
                StringComparison.Ordinal))
        {
            return 3;
        }

        if (string.Equals(
                key,
                "CAN",
                StringComparison.Ordinal) ||
            key.Contains(
                "CANYAYIN",
                StringComparison.Ordinal))
        {
            return 4;
        }

        if (string.Equals(
                key,
                "ALFA",
                StringComparison.Ordinal) ||
            key.Contains(
                "ALFAYAYIN",
                StringComparison.Ordinal))
        {
            return 5;
        }

        if (key.Contains(
                "DOGUBATI",
                StringComparison.Ordinal))
        {
            return 6;
        }

        return int.MaxValue;
    }

    private static string NormalizePublisherKey(
        string publisherName)
    {
        string decomposedName =
            publisherName
                .Trim()
                .Normalize(
                    NormalizationForm.FormD);

        StringBuilder builder = new(
            decomposedName.Length);

        foreach (char character in decomposedName)
        {
            UnicodeCategory category =
                CharUnicodeInfo.GetUnicodeCategory(
                    character);

            if (category ==
                UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                char normalizedCharacter =
                    character is 'ı' or 'İ'
                        ? 'I'
                        : char.ToUpperInvariant(
                            character);

                builder.Append(
                    normalizedCharacter);
            }
        }

        return builder.ToString();
    }

    private static bool IsTurkishLanguage(
        string? languageCode)
    {
        return string.Equals(
                   languageCode,
                   "tr",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   languageCode,
                   "tur",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeLanguageCode(
        string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(
                languageCode))
        {
            return null;
        }

        return languageCode
            .Trim()
            .ToLowerInvariant() switch
        {
            "tr" or "tur" => "tur",
            var normalizedCode => normalizedCode
        };
    }

    private static string NormalizeWorkId(
        string externalWorkId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            externalWorkId);

        Match match = WorkIdPattern.Match(
            externalWorkId.Trim());

        if (!match.Success)
        {
            throw new ArgumentException(
                "Open Library work identifier is invalid.",
                nameof(externalWorkId));
        }

        string workId =
            match.Groups[1]
                .Value
                .ToUpperInvariant();

        return $"/works/{workId}";
    }

    private static string? TryNormalizeEditionId(
        string? externalEditionId)
    {
        if (string.IsNullOrWhiteSpace(
                externalEditionId))
        {
            return null;
        }

        Match match = EditionIdPattern.Match(
            externalEditionId.Trim());

        if (!match.Success)
        {
            return null;
        }

        string editionId =
            match.Groups[1]
                .Value
                .ToUpperInvariant();

        return $"/books/{editionId}";
    }

    private static string? MapLanguageCode(
        IEnumerable<
            OpenLibraryEditionLanguageResponse>
            languages)
    {
        foreach (
            OpenLibraryEditionLanguageResponse
                language in languages)
        {
            if (string.IsNullOrWhiteSpace(
                    language.Key))
            {
                continue;
            }

            Match match = LanguageKeyPattern.Match(
                language.Key.Trim());

            if (match.Success)
            {
                string languageCode =
                    match.Groups[1]
                        .Value
                        .ToLowerInvariant();

                if (IsTurkishLanguage(
                        languageCode))
                {
                    return "tur";
                }
            }
        }

        return null;
    }

    private static string? FindIsbn10(
        IEnumerable<string> values)
    {
        return values
            .Select(NormalizeIsbn)
            .FirstOrDefault(value =>
                value is not null &&
                Isbn10Pattern.IsMatch(value));
    }

    private static string? FindIsbn13(
        IEnumerable<string> values)
    {
        return values
            .Select(NormalizeIsbn)
            .FirstOrDefault(value =>
                value is not null &&
                Isbn13Pattern.IsMatch(value));
    }

    private static string? NormalizeIsbn(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Replace(
                "-",
                string.Empty,
                StringComparison.Ordinal)
            .Replace(
                " ",
                string.Empty,
                StringComparison.Ordinal)
            .Trim()
            .ToUpperInvariant();
    }

    private static int? MapPublicationYear(
        string? publishDate)
    {
        if (string.IsNullOrWhiteSpace(
                publishDate))
        {
            return null;
        }

        Match match = YearPattern.Match(
            publishDate);

        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(
            match.Value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int year)
                ? year
                : null;
    }

    private static string? MapCoverUrl(
        IEnumerable<int> coverIds)
    {
        int? coverId = coverIds
            .Cast<int?>()
            .FirstOrDefault(identifier =>
                identifier > 0);

        return coverId.HasValue
            ? "https://covers.openlibrary.org/" +
              $"b/id/{coverId.Value}-L.jpg"
            : null;
    }

    private static string? FindPublisherName(
        IEnumerable<string> publishers)
    {
        return publishers
            .Select((publisher, index) => new
            {
                Name = NormalizeOptionalValue(
                    publisher),
                Index = index
            })
            .Where(publisher =>
                publisher.Name is not null)
            .OrderBy(publisher =>
                GetPublisherPriority(
                    publisher.Name!))
            .ThenBy(publisher =>
                publisher.Index)
            .Select(publisher =>
                publisher.Name)
            .FirstOrDefault();
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record
        OpenLibraryEditionCandidate(
            OpenLibraryEditionResponse Edition,
            string ExternalId,
            string LanguageCode);
}