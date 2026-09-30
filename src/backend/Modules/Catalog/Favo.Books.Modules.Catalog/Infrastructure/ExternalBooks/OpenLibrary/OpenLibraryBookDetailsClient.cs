using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary.Contracts;

namespace Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

internal sealed class OpenLibraryBookDetailsClient(
    HttpClient httpClient,
    IExternalBookSearchClient searchClient)
    : IExternalBookDetailsClient
{
    private const string ProviderName =
        "OpenLibrary";

    private readonly HttpClient _httpClient =
        httpClient;

    private readonly IExternalBookSearchClient
        _searchClient = searchClient;

    public async Task<ExternalBookDetails?> GetByIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        string requestedWorkId =
            NormalizeWorkId(externalId);

        string requestUri =
            $"{requestedWorkId.TrimStart('/')}.json";

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

        if (response.StatusCode ==
            HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        OpenLibraryWorkResponse? work =
            await response.Content
                .ReadFromJsonAsync<
                    OpenLibraryWorkResponse>(
                    cancellationToken);

        if (work is null)
        {
            throw new JsonException(
                "Open Library returned an empty work response.");
        }

        string workId =
            string.IsNullOrWhiteSpace(work.Key)
                ? requestedWorkId
                : NormalizeWorkId(work.Key);

        ExternalBookSearchResult searchResult =
            await _searchClient.SearchAsync(
                $"key:{workId}",
                page: 1,
                pageSize: 1,
                cancellationToken);

        ExternalBookSearchItem? searchItem =
            searchResult.Items.FirstOrDefault(item =>
                string.Equals(
                    item.ExternalId,
                    workId,
                    StringComparison.OrdinalIgnoreCase));

        string? title =
            NormalizeOptionalValue(work.Title)
            ?? searchItem?.Title;

        if (title is null)
        {
            throw new JsonException(
                "Open Library work title was missing.");
        }

        IReadOnlyList<ExternalBookAuthor>
            workAuthors =
                await GetAuthorsAsync(
                    work.Authors,
                    cancellationToken);

        IReadOnlyList<ExternalBookAuthor> authors =
            workAuthors.Count > 0
                ? workAuthors
                : searchItem?.Authors ?? [];

        return new ExternalBookDetails(
            ProviderName,
            workId,
            title,
            MapDescription(work.Description),
            authors,
            searchItem?.FirstPublishedYear
                ?? MapFirstPublishedYear(
                    work.FirstPublishDate),
            MapCoverUrl(work.CoverIds)
                ?? searchItem?.CoverUrl,
            searchItem?.Isbns ?? [],
            searchItem?.LanguageCodes ?? [],
            NormalizeValues(work.Subjects));
    }

    private async Task<
     IReadOnlyList<ExternalBookAuthor>>
     GetAuthorsAsync(
         IEnumerable<
             OpenLibraryWorkAuthorResponse>
             workAuthors,
         CancellationToken cancellationToken)
    {
        List<OpenLibraryResolvedAuthor>
            resolvedAuthors = [];

        HashSet<string> resolvedAuthorKeys =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (
            OpenLibraryWorkAuthorResponse workAuthor
            in workAuthors)
        {
            string? authorKey =
                NormalizeAuthorKey(
                    workAuthor.Author?.Key);

            if (authorKey is null ||
                !resolvedAuthorKeys.Add(authorKey))
            {
                continue;
            }

            string requestUri =
                $"{authorKey.TrimStart('/')}.json";

            using HttpResponseMessage response =
                await _httpClient.GetAsync(
                    requestUri,
                    cancellationToken);

            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                continue;
            }

            response.EnsureSuccessStatusCode();

            OpenLibraryAuthorResponse? author =
                await response.Content
                    .ReadFromJsonAsync<
                        OpenLibraryAuthorResponse>(
                        cancellationToken);

            string? authorName =
                NormalizeOptionalValue(
                    author?.Name);

            if (authorName is null)
            {
                continue;
            }

            IReadOnlyList<string> alternativeNames =
                NormalizeValues(
                    author?.AlternativeNames ?? []);

            resolvedAuthors.Add(
                new OpenLibraryResolvedAuthor(
                    authorKey,
                    authorName,
                    alternativeNames));
        }

        return OpenLibraryAuthorDeduplicator
            .Deduplicate(resolvedAuthors);
    }

    private static string NormalizeWorkId(
        string externalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            externalId);

        string normalizedExternalId =
            externalId.Trim();

        if (!normalizedExternalId.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            normalizedExternalId =
                $"/{normalizedExternalId}";
        }

        if (!normalizedExternalId.StartsWith(
                "/works/",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Open Library work identifier must start with '/works/'.",
                nameof(externalId));
        }

        return normalizedExternalId;
    }

    private static string? NormalizeAuthorKey(
        string? authorKey)
    {
        if (string.IsNullOrWhiteSpace(authorKey))
        {
            return null;
        }

        string normalizedKey =
            authorKey.Trim();

        if (!normalizedKey.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            normalizedKey =
                $"/{normalizedKey}";
        }

        Match match = Regex.Match(
            normalizedKey,
            @"^/authors/(OL\d+A)$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return null;
        }

        string authorId =
            match.Groups[1]
                .Value
                .ToUpperInvariant();

        return $"/authors/{authorId}";
    }

    private static string? MapDescription(
        JsonElement? description)
    {
        if (!description.HasValue)
        {
            return null;
        }

        JsonElement element =
            description.Value;

        string? value = null;

        if (element.ValueKind ==
            JsonValueKind.String)
        {
            value = element.GetString();
        }
        else if (
            element.ValueKind ==
                JsonValueKind.Object &&
            element.TryGetProperty(
                "value",
                out JsonElement valueElement) &&
            valueElement.ValueKind ==
                JsonValueKind.String)
        {
            value = valueElement.GetString();
        }

        return NormalizeOptionalValue(value);
    }

    private static int? MapFirstPublishedYear(
        string? firstPublishDate)
    {
        if (string.IsNullOrWhiteSpace(
                firstPublishDate))
        {
            return null;
        }

        Match match = Regex.Match(
            firstPublishDate,
            @"(?<!\d)\d{4}(?!\d)",
            RegexOptions.CultureInvariant);

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
            .FirstOrDefault(id => id > 0);

        return coverId.HasValue
            ? "https://covers.openlibrary.org/" +
              $"b/id/{coverId.Value}-L.jpg"
            : null;
    }

    private static IReadOnlyList<string>
        NormalizeValues(
            IEnumerable<string> values)
    {
        return values
            .Where(value =>
                !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}