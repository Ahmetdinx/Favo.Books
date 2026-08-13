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

    private const string SearchFields =
        "key,editions,editions.key";

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

        string searchRequestUri =
            CreateSearchRequestUri(workId);

        OpenLibraryEditionSearchResponse?
            searchResponse =
                await _httpClient.GetFromJsonAsync<
                    OpenLibraryEditionSearchResponse>(
                    searchRequestUri,
                    cancellationToken);

        if (searchResponse is null)
        {
            throw new JsonException(
                "Open Library returned an empty " +
                "edition search response.");
        }

        OpenLibraryEditionSearchDocument?
            workDocument =
                searchResponse.Documents
                    .FirstOrDefault(document =>
                        string.Equals(
                            TryNormalizeWorkId(
                                document.Key),
                            workId,
                            StringComparison
                                .OrdinalIgnoreCase));

        string? editionId =
            workDocument?.Editions?.Documents
                .Select(document =>
                    TryNormalizeEditionId(
                        document.Key))
                .FirstOrDefault(identifier =>
                    identifier is not null);

        if (editionId is null)
        {
            return null;
        }

        string editionRequestUri =
            $"{editionId.TrimStart('/')}.json";

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                editionRequestUri,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        OpenLibraryEditionResponse? edition =
            await response.Content
                .ReadFromJsonAsync<
                    OpenLibraryEditionResponse>(
                    cancellationToken);

        if (edition is null)
        {
            throw new JsonException(
                "Open Library returned an empty " +
                "edition response.");
        }

        string resolvedEditionId =
            TryNormalizeEditionId(edition.Key)
            ?? editionId;

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

    private static string CreateSearchRequestUri(
        string workId)
    {
        string query =
            Uri.EscapeDataString(
                $"key:{workId}");

        string fields =
            Uri.EscapeDataString(
                SearchFields);

        return
            $"search.json?q={query}" +
            "&page=1" +
            "&limit=1" +
            $"&fields={fields}";
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