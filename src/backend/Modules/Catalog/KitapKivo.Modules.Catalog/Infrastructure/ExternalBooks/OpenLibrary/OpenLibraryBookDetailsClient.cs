using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

using KitapKivo.Modules.Catalog.Application.Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Infrastructure.ExternalBooks
    .OpenLibrary.Contracts;

namespace KitapKivo.Modules.Catalog.Infrastructure.ExternalBooks
    .OpenLibrary;

internal sealed class OpenLibraryBookDetailsClient(
    HttpClient httpClient,
    IExternalBookSearchClient searchClient)
    : IExternalBookDetailsClient
{
    private const string ProviderName = "OpenLibrary";

    private readonly HttpClient _httpClient = httpClient;

    private readonly IExternalBookSearchClient _searchClient =
        searchClient;

    public async Task<ExternalBookDetails?> GetByIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        string requestedWorkId = NormalizeWorkId(externalId);

        string requestUri =
            $"{requestedWorkId.TrimStart('/')}.json";

        using HttpResponseMessage response =
            await _httpClient.GetAsync(
                requestUri,
                cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        OpenLibraryWorkResponse? work =
            await response.Content
                .ReadFromJsonAsync<OpenLibraryWorkResponse>(
                    cancellationToken);

        if (work is null)
        {
            throw new JsonException(
                "Open Library returned an empty work response.");
        }

        string workId = string.IsNullOrWhiteSpace(work.Key)
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

        string? title = NormalizeOptionalValue(work.Title)
            ?? searchItem?.Title;

        if (title is null)
        {
            throw new JsonException(
                "Open Library work title was missing.");
        }

        return new ExternalBookDetails(
            ProviderName,
            workId,
            title,
            MapDescription(work.Description),
            searchItem?.Authors ?? [],
            searchItem?.FirstPublishedYear
                ?? MapFirstPublishedYear(
                    work.FirstPublishDate),
            MapCoverUrl(work.CoverIds)
                ?? searchItem?.CoverUrl,
            searchItem?.Isbns ?? [],
            searchItem?.LanguageCodes ?? [],
            NormalizeValues(work.Subjects));
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

    private static string? MapDescription(
        JsonElement? description)
    {
        if (!description.HasValue)
        {
            return null;
        }

        JsonElement element = description.Value;
        string? value = null;

        if (element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString();
        }
        else if (
            element.ValueKind == JsonValueKind.Object &&
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
            ? $"https://covers.openlibrary.org/b/id/" +
              $"{coverId.Value}-L.jpg"
            : null;
    }

    private static IReadOnlyList<string> NormalizeValues(
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