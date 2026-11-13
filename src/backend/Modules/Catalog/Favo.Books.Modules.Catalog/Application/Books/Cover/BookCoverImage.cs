using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Application.Books.Cover;

public sealed record BookCoverImageResult(byte[] Content, string ContentType);

/// <summary>
/// Serves a visible novel's cover from our own origin so the browser may draw it into a share card
/// (the publishers' image hosts send no CORS headers). Only URLs stored in the catalog are fetched,
/// and only from known cover hosts, so the endpoint cannot be used to reach arbitrary addresses.
/// </summary>
public sealed class BookCoverImage(CatalogDbContext db, IHttpClientFactory httpClients)
{
    public const string HttpClientName = "catalog-covers";
    const int MaxBytes = 4 * 1024 * 1024;

    static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "img.kitapyurdu.com", "www.dogankitap.com.tr", "img.iskultur.com.tr",
        "wwwithakiyayingrubucom.sm.mncdn.com", "static.nadirkitap.com", "covers.openlibrary.org"
    };

    public async Task<BookCoverImageResult?> GetAsync(Guid bookId, CancellationToken cancellationToken)
    {
        // Same edition choice as the discovery catalog: the newest Turkish edition with a cover.
        string? source = await db.Books.AsNoTracking().EligibleNovels()
            .Where(book => book.Id == bookId)
            .Select(book => book.Editions
                .Where(e => (e.LanguageCode == "tur" || e.LanguageCode == "tr") && e.CoverImageUrl != null && e.CoverImageUrl != "")
                .OrderByDescending(e => e.PublicationYear).ThenBy(e => e.Id)
                .Select(e => e.CoverImageUrl)
                .FirstOrDefault())
            .FirstOrDefaultAsync(cancellationToken);

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !uri.IsDefaultPort || !string.IsNullOrEmpty(uri.UserInfo) || !AllowedHosts.Contains(uri.Host))
            return null;

        using var response = await httpClients.CreateClient(HttpClientName)
            .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        string? contentType = response.Content.Headers.ContentType?.MediaType;
        if (!response.IsSuccessStatusCode || contentType is null || !contentType.StartsWith("image/", StringComparison.Ordinal)
            || response.Content.Headers.ContentLength > MaxBytes)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxBytes) return null;
            buffer.Write(chunk, 0, read);
        }
        return new(buffer.ToArray(), contentType);
    }
}
