using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

using Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary.Contracts;

namespace Favo.Books.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary;

internal sealed class OpenLibrarySearchClient(
    HttpClient httpClient) : IExternalBookSearchClient
{
    private const string ProviderName = "OpenLibrary";

    private const string SearchFields =
        "key,title,author_name,author_key,first_publish_year," +
        "cover_i,isbn,language";

    private readonly HttpClient _httpClient = httpClient;

    public async Task<ExternalBookSearchResult> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        string requestUri = CreateSearchUri(
            query,
            page,
            pageSize);

        OpenLibrarySearchResponse? response =
            await _httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(
                requestUri,
                cancellationToken);

        if (response is null)
        {
            throw new JsonException(
                "Open Library returned an empty search response.");
        }

        ExternalBookSearchItem[] items = response.Documents
            .Select(MapDocument)
            .Where(item => item is not null)
            .Cast<ExternalBookSearchItem>()
            .ToArray();

        return new ExternalBookSearchResult(
            response.NumFound,
            page,
            pageSize,
            items);
    }

    private static string CreateSearchUri(
        string query,
        int page,
        int pageSize)
    {
        return
            $"search.json?q={Uri.EscapeDataString(query)}" +
            $"&page={page.ToString(CultureInfo.InvariantCulture)}" +
            $"&limit={pageSize.ToString(CultureInfo.InvariantCulture)}" +
            $"&fields={Uri.EscapeDataString(SearchFields)}";
    }

    private static ExternalBookSearchItem? MapDocument(
        OpenLibrarySearchDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Key) ||
            string.IsNullOrWhiteSpace(document.Title))
        {
            return null;
        }

        IReadOnlyList<ExternalBookAuthor> authors =
            MapAuthors(document);

        string? coverUrl = document.CoverId is > 0
            ? $"https://covers.openlibrary.org/b/id/" +
              $"{document.CoverId}-M.jpg"
            : null;

        return new ExternalBookSearchItem(
            ProviderName,
            document.Key.Trim(),
            document.Title.Trim(),
            authors,
            document.FirstPublishYear,
            coverUrl,
            NormalizeValues(document.Isbns),
            NormalizeValues(document.LanguageCodes));
    }

    private static IReadOnlyList<ExternalBookAuthor> MapAuthors(
        OpenLibrarySearchDocument document)
    {
        var authors = new List<ExternalBookAuthor>();

        for (int index = 0;
             index < document.AuthorNames.Length;
             index++)
        {
            string authorName = document.AuthorNames[index];

            if (string.IsNullOrWhiteSpace(authorName))
            {
                continue;
            }

            string? authorId =
                index < document.AuthorKeys.Length &&
                !string.IsNullOrWhiteSpace(
                    document.AuthorKeys[index])
                    ? document.AuthorKeys[index].Trim()
                    : null;

            authors.Add(new ExternalBookAuthor(
                authorId,
                authorName.Trim()));
        }

        return authors;
    }

    private static IReadOnlyList<string> NormalizeValues(
        IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}