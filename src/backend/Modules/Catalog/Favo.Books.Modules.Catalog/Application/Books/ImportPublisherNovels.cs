using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Favo.Books.Modules.Catalog.Application.Categories;
using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Domain.Publishers;
using Favo.Books.Modules.Catalog.Domain.Sources;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Application.Books;

public sealed record PublisherNovel(string Title, string[] AuthorNames, string Isbn13,
    string CoverImageUrl, string SourceUrl, string SourceCategory, string Publisher,
    string LanguageCode, string OriginalLanguageCode, int? PageCount, int? PublicationYear,
    int Priority, string[] CategorySlugs, string SourceSha256, string ListUrl, string? ReviewNote = null);
public sealed record PublisherImportItem(string SourceUrl, string Title, string Isbn13, Guid BookId);
public sealed record PublisherImportResult(bool Applied, int AvailableNew, int ExistingOrDuplicate,
    IReadOnlyList<PublisherImportItem> Items);

/// <summary>Trusted offline publisher manifest import. Not registered as a service or HTTP route.
/// The operator must verify cached source facts and cover responses before applying.</summary>
public sealed class ImportPublisherNovels(CatalogDbContext db)
{
    public async Task<(int TotalBooks, int EligibleBooks)> GetCountsAsync()
        => (await db.Books.CountAsync(), await db.Books.EligibleNovels().CountAsync());

    public async Task<PublisherImportResult> ExecuteAsync(PublisherNovel[] entries, int target,
        string batchId, bool apply, CancellationToken ct = default)
    {
        if (target is < 1 or > 10000 || entries is null || entries.Length is < 1 or > 10000)
            throw new ArgumentException("Invalid target or manifest size.");
        if (string.IsNullOrWhiteSpace(batchId) || batchId.Length > 100)
            throw new ArgumentException("A stable batch ID is required.");
        foreach (var entry in entries) Validate(entry);
        if (entries.Select(e=>e.SourceUrl).Distinct(StringComparer.Ordinal).Count()!=entries.Length)
            throw new ArgumentException("Duplicate publisher source URLs in manifest.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(842619001)", ct);
        // A repeat of the same batch reports its existing rows instead of filling the
        // target with another set of books. Never repurpose a batch ID.
        var prior = await db.NovelReviews.AsNoTracking().Where(r => r.BatchId == batchId)
            .Select(r => new { r.BookId, r.SourceUrl, r.EvidenceNote }).ToArrayAsync(ct);
        if (prior.Length > 0)
        {
            if (prior.Length != target || prior.Any(p => !entries.Any(e => e.SourceUrl == p.SourceUrl)))
                throw new InvalidOperationException("Batch ID already belongs to a different import.");
            var priorIds=prior.Select(p=>p.BookId).ToArray();
            var stored=await db.Books.AsNoTracking().Where(b=>priorIds.Contains(b.Id))
                .Select(b=>new { b.Id,b.Title,Isbns=b.Editions.Select(e=>e.Isbn13).ToArray() }).ToArrayAsync(ct);
            if (prior.Any(p=> !entries.Any(e=>e.SourceUrl==p.SourceUrl && p.EvidenceNote.Contains(e.SourceSha256)
                && stored.Any(b=>b.Id==p.BookId && b.Title==e.Title && b.Isbns.Contains(e.Isbn13)))))
                throw new InvalidOperationException("Existing batch content does not match this manifest.");
            var priorItems = prior.Select(p =>
            {
                var e = entries.Single(e => e.SourceUrl == p.SourceUrl);
                return new PublisherImportItem(e.SourceUrl, e.Title, e.Isbn13, p.BookId);
            }).ToArray();
            return new(false, 0, prior.Length, priorItems);
        }
        var titles = (await db.Books.AsNoTracking().Select(b => b.Title).ToArrayAsync(ct))
            .Concat(await db.Books.AsNoTracking().Where(b => b.OriginalTitle != null).Select(b => b.OriginalTitle!).ToArrayAsync(ct))
            .Concat(await db.Editions.AsNoTracking().Select(e => e.Title).ToArrayAsync(ct))
            .Select(TitleKey).ToHashSet(StringComparer.Ordinal);
        var isbns = (await db.Editions.AsNoTracking().Where(e => e.Isbn13 != null).Select(e => e.Isbn13!).ToArrayAsync(ct)).ToHashSet();
        foreach (var isbn in await db.Editions.AsNoTracking().Where(e => e.Isbn10 != null).Select(e => e.Isbn10!).ToArrayAsync(ct))
            if (isbn.Length == 10 && isbn[..9].All(char.IsDigit))
            {
                var prefix = "978" + isbn[..9];
                isbns.Add(prefix + ((10 - prefix.Select((c,i) => (c-'0') * (i%2==0 ? 1 : 3)).Sum()%10)%10));
            }
        var urls = (await db.ExternalReferences.AsNoTracking().Where(r => r.Source == CatalogSource.PublisherFeed)
            .Select(r => r.ExternalId).ToArrayAsync(ct)).ToHashSet();
        var selected = new List<PublisherNovel>();
        var duplicates = 0;
        foreach (var entry in entries.OrderBy(e => e.Priority).ThenBy(e => e.SourceUrl, StringComparer.Ordinal))
        {
            if (titles.Contains(TitleKey(entry.Title)) || isbns.Contains(entry.Isbn13) || urls.Contains(entry.SourceUrl))
            { duplicates++; continue; }
            titles.Add(TitleKey(entry.Title)); isbns.Add(entry.Isbn13); urls.Add(entry.SourceUrl);
            selected.Add(entry);
        }
        if (apply && selected.Count < target)
            throw new InvalidOperationException($"Only {selected.Count} verified new works available; target {target}. No changes applied.");
        if (!apply)
            return new(false, selected.Count, duplicates, selected.Take(target)
                .Select(e => new PublisherImportItem(e.SourceUrl,e.Title,e.Isbn13,Guid.Empty)).ToArray());
        var authors = await db.Authors.ToListAsync(ct);
        var publishers = await db.Publishers.ToListAsync(ct);
        var categories = await db.Categories.ToDictionaryAsync(c => c.Slug,ct);
        var output = new List<PublisherImportItem>();
        foreach (var e in selected.Take(target))
        {
            // Doğan's hidden original-language field defaults to Turkish even on
            // translated works. Do not turn that default into a bibliographic fact.
            var originalLanguage = new Uri(e.SourceUrl).Host == "www.dogankitap.com.tr" ? "und" : e.OriginalLanguageCode;
            var book = new Book(e.Title,originalLanguage);
            for (var i=0; i<e.AuthorNames.Length; i++)
            {
                var name=e.AuthorNames[i].Trim();
                var author=authors.FirstOrDefault(a => a.Name.Equals(name,StringComparison.OrdinalIgnoreCase));
                if (author is null) { author=new Author(name); db.Authors.Add(author); authors.Add(author); }
                book.Authors.Add(new BookAuthor(book.Id,author.Id,i));
            }
            var publisher=publishers.FirstOrDefault(p => p.Name==e.Publisher);
            if (publisher is null) { publisher=new Publisher(e.Publisher); db.Publishers.Add(publisher); publishers.Add(publisher); }
            book.Editions.Add(new Edition(book.Id,e.Title,"tur",EditionFormat.Unknown,
                publisher.Id,isbn13:e.Isbn13,pageCount:e.PageCount,publicationYear:e.PublicationYear,coverImageUrl:e.CoverImageUrl));
            foreach (var slug in e.CategorySlugs.Distinct())
                book.Categories.Add(new Domain.Categories.BookCategory(book.Id,categories[slug].Id));
            book.ExternalReferences.Add(ExternalReference.CreateForBook(book.Id,CatalogSource.PublisherFeed,e.SourceUrl,e.SourceUrl));
            book.NovelReviews.Add(new NovelReview(book.Id,NovelDecision.Novel,batchId,e.SourceUrl,
                $"Publisher product classification: {e.SourceCategory}. Turkish edition ISBN {e.Isbn13}; source SHA256 {e.SourceSha256}. Covered edition checked before import. {e.ReviewNote}",
                "publisher-source-review", "publisher-novels-v1"));
            db.Books.Add(book);
            output.Add(new(e.SourceUrl,e.Title,e.Isbn13,book.Id));
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(true,selected.Count,duplicates,output);
    }

    public static string TitleKey(string value)
    {
        value=Regex.Replace(value, @"\([^)]*\)|\[[^\]]*\]", " ");
        value=Regex.Replace(value, @"\s*[–—-]\s*(sert kapak|ciltli|karton kapak|özel baskı).*$", "", RegexOptions.IgnoreCase);
        value=Regex.Replace(value, @"(?:\s*[-–—]\s*|\s+)cep(\s+kitabı)?$", "", RegexOptions.IgnoreCase);
        value=Regex.Replace(value, @"\s+\d+\s+cilt$", "", RegexOptions.IgnoreCase);
        var decomposed=value.Replace('ı','i').Replace('İ','i').Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var key=new string(decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c)).ToArray());
        // Known alternate titles of already reviewed works, not new editions.
        return key switch
        {
            "bindokuzyuzseksendort" or "nineteeneightyfour" => "1984",
            "mobydickyadabalina" or "mobydickorthewhale" => "mobydick",
            "1q841kitap" or "1q842kitap" or "1q843kitap" => "1q84",
            "3900tl" or "4900tl" or "990" => "99francs",
            _ => key
        };
    }

    public static void Validate(PublisherNovel e)
    {
        if (e is null || string.IsNullOrWhiteSpace(e.Title) || e.Title.Length>500 || TitleKey(e.Title).Length==0
            || e.AuthorNames is not { Length: >0 and <= 4 } || e.AuthorNames.Any(a => string.IsNullOrWhiteSpace(a) || a.Length>300)
            || e.AuthorNames.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=e.AuthorNames.Length)
            throw new ArgumentException("Missing or invalid bibliographic identity.");
        if (e.Isbn13 is null || !Regex.IsMatch(e.Isbn13,@"^97[89]\d{10}$")
            || e.Isbn13.Select((c,i)=>(c-'0')*(i%2==0?1:3)).Sum()%10!=0)
            throw new ArgumentException("Invalid ISBN checksum.");
        if (e.LanguageCode!="tur" || e.OriginalLanguageCode is null || !Regex.IsMatch(e.OriginalLanguageCode,@"^[a-z]{3}$"))
            throw new ArgumentException("A Turkish edition and an explicit original-language code (or und) are required.");
        var source = Https(e.SourceUrl); var cover=Https(e.CoverImageUrl); var list=Https(e.ListUrl);
        var dogan=source.Host=="www.dogankitap.com.tr" && source.AbsolutePath.StartsWith("/kitap/");
        var iskultur=source.Host=="www.iskultur.com.tr" && source.AbsolutePath.EndsWith(".aspx");
        var ithaki=source.Host=="www.ithakiyayingrubu.com";
        if (ithaki && (e.SourceCategory!="Türk Edebiyatı-Roman"
            || e.Publisher is not ("İthaki Yayınları" or "Müptela Yayınları")
            || cover.Host!="wwwithakiyayingrubucom.sm.mncdn.com"))
            throw new ArgumentException("Publisher provenance mismatch.");
        if (dogan && (e.SourceCategory!="Romanlar" || e.Publisher is not ("Doğan Kitap" or "Doğan Novus" or "Doğan SoLibri") || cover.Host!="www.dogankitap.com.tr"))
            throw new ArgumentException("Publisher provenance mismatch.");
        if (iskultur && (!Regex.IsMatch(e.SourceCategory,@"(^|/)\s*Roman\s*($|/)",RegexOptions.IgnoreCase)
            || e.Publisher!="İş Bankası Kültür Yayınları" || cover.Host!="img.iskultur.com.tr"))
            throw new ArgumentException("Publisher provenance mismatch.");
        if ((!dogan && !iskultur && !ithaki) || list.Host!=source.Host || e.SourceUrl.Length>500 || e.CoverImageUrl.Length>2000
            || e.SourceSha256 is null || !Regex.IsMatch(e.SourceSha256,@"^[a-fA-F0-9]{64}$")
            || e.CategorySlugs is null || e.CategorySlugs.Any(s => !NovelGenreCatalog.Slugs.Contains(s))
            || e.ReviewNote is { Length: >1000 } || e.PageCount is <=0 || e.PublicationYear is <1450 or >2100)
            throw new ArgumentException("Untrusted or incomplete publisher review.");
    }
    private static Uri Https(string url)
        => Uri.TryCreate(url,UriKind.Absolute,out var uri) && uri.Scheme=="https" && uri.IsDefaultPort && uri.UserInfo.Length==0
            ? uri : throw new ArgumentException("A public HTTPS source URL is required.");
}
