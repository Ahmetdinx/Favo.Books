using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary.Contracts;

namespace KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

internal sealed class OpenLibraryBookEditionClient(
    HttpClient httpClient)
    : IExternalBookEditionClient
{
    private const string ProviderName =
        "OpenLibrary";

    private const int EditionCandidateLimit = 20;

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
        string workId =
            NormalizeWorkId(externalWorkId);

        string editionsRequestUri =
            CreateEditionsRequestUri(workId);

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                editionsRequestUri,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
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

        var bestCandidate = editionsResponse.Entries
            .Select(edition => new
            {
                Edition = edition,
                EditionId =
                    TryNormalizeEditionId(
                        edition.Key)
            })
            .Where(candidate =>
                candidate.EditionId is not null)
            .OrderByDescending(candidate =>
                CalculateEditionQualityScore(
                    candidate.Edition))
            .ThenByDescending(candidate =>
                MapPublicationYear(
                    candidate.Edition.PublishDate))
            .ThenBy(candidate =>
                candidate.EditionId)
            .FirstOrDefault();

        if (bestCandidate is null)
        {
            return null;
        }

        OpenLibraryEditionResponse edition =
            bestCandidate.Edition;

        string resolvedEditionId =
            bestCandidate.EditionId!;

        return new ExternalBookEditionDetails(
            ProviderName,
            resolvedEditionId,
            NormalizeOptionalValue(
                edition.Title),
            MapLanguageCode(
                edition.Languages),
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

    private static string CreateEditionsRequestUri(
        string workId)
    {
        return
            $"{workId.TrimStart('/')}/editions.json" +
            $"?limit={EditionCandidateLimit}";
    }

    private static int CalculateEditionQualityScore(
        OpenLibraryEditionResponse edition)
    {
        int score = 0;

        if (MapCoverUrl(edition.CoverIds) is not null)
        {
            score += 200;
        }

        string? languageCode =
            MapLanguageCode(edition.Languages);

        if (IsTurkishLanguage(languageCode))
        {
            score += 60;
        }
        else if (string.Equals(
                     languageCode,
                     "eng",
                     StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        if (FindIsbn13(edition.Isbn13Values) is not null)
        {
            score += 35;
        }

        if (FindIsbn10(edition.Isbn10Values) is not null)
        {
            score += 20;
        }

        if (edition.NumberOfPages is > 0)
        {
            score += 15;
        }

        if (FindPublisherName(
                edition.Publishers) is not null)
        {
            score += 15;
        }

        if (NormalizeOptionalValue(
                edition.PhysicalFormat) is not null)
        {
            score += 5;
        }

        if (MapPublicationYear(
                edition.PublishDate).HasValue)
        {
            score += 5;
        }

        if (NormalizeOptionalValue(
                edition.Title) is not null)
        {
            score += 5;
        }

        return score;
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

    private static string NormalizeWorkId(
        string externalWorkId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            externalWorkId);

        string? normalizedWorkId =
            TryNormalizeWorkId(
                externalWorkId);

        if (normalizedWorkId is null)
        {
            throw new ArgumentException(
                "Open Library work identifier is invalid.",
                nameof(externalWorkId));
        }

        return normalizedWorkId;
    }

    private static string? TryNormalizeWorkId(
        string? externalWorkId)
    {
        if (string.IsNullOrWhiteSpace(
                externalWorkId))
        {
            return null;
        }

        Match match = WorkIdPattern.Match(
            externalWorkId.Trim());

        if (!match.Success)
        {
            return null;
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
                return match.Groups[1]
                    .Value
                    .ToLowerInvariant();
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
            .Select(NormalizeOptionalValue)
            .FirstOrDefault(publisher =>
                publisher is not null);
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}