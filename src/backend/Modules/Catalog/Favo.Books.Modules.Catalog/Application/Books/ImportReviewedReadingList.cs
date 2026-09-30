using System.Security.Cryptography;
using System.Text;
using Favo.Books.Modules.Catalog.Domain.Authors;
using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Domain.Publishers;
using Favo.Books.Modules.Catalog.Domain.Sources;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Application.Books;

public sealed record ReviewedReadingListEntry(int Number, string Title, string Author, string EditionTitle,
    string? Isbn13, string CoverImageUrl, string Publisher, int? PageCount, string SourceUrl,
    string SourceSha256, string LanguageCode, string EvidenceNote, NovelDecision Decision,
    Guid? ExistingBookId = null, string? ExpectedTitle = null, string? ExpectedAuthor = null);
public sealed record ReadingListImportItem(int Number, Guid BookId, string Title, bool Created, bool Visible);

/// <summary>Offline, explicitly reviewed list import. Does not register a public write endpoint.</summary>
public sealed class ImportReviewedReadingList(CatalogDbContext db)
{
    public async Task<ReadingListImportItem[]> ExecuteAsync(ReviewedReadingListEntry[] entries,
        string batchId, bool apply, CancellationToken ct = default)
    {
        if (entries.Length is < 1 or > 1000 || entries.Select(e => e.Number).Distinct().Count() != entries.Length
            || entries.Where(e => e.Isbn13 != null).Select(e => e.Isbn13).Distinct().Count() != entries.Count(e => e.Isbn13 != null)
            || entries.Select(e => e.SourceUrl).Distinct().Count() != entries.Length
            || entries.Where(e => e.ExistingBookId != null).Select(e => e.ExistingBookId).Distinct().Count()
                != entries.Count(e => e.ExistingBookId != null)) throw new ArgumentException("Duplicate or invalid list.");
        foreach (var e in entries) Validate(e);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(842619001)", ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(842619002)", ct);
        var output = new List<ReadingListImportItem>();
        var authors = await db.Authors.ToListAsync(ct);
        var publishers = await db.Publishers.ToListAsync(ct);
        var classic = await db.Categories.SingleAsync(c => c.Slug == "klasikler", ct);
        foreach (var e in entries.OrderBy(e => e.Number))
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                System.Text.Json.JsonSerializer.Serialize(e))));
            var prior = await db.NovelReviews.SingleOrDefaultAsync(r => r.BatchId == batchId
                && r.SourceUrl == e.SourceUrl, ct);
            if (prior != null)
            {
                if (!prior.EvidenceNote.Contains(fingerprint)) throw new InvalidOperationException("Batch content changed.");
                var visible = await db.Books.EligibleNovels().AnyAsync(b => b.Id == prior.BookId, ct);
                if (!visible) throw new InvalidOperationException("Previously imported book is no longer visible.");
                output.Add(new(e.Number, prior.BookId, e.Title, false, true));
                continue;
            }
            var book = e.ExistingBookId is { } id ? await db.Books.Include(b => b.Authors).ThenInclude(a => a.Author)
                .Include(b => b.Editions).Include(b => b.Categories).AsSplitQuery().SingleAsync(b => b.Id == id, ct) : null;
            if (book != null && (book.Title != e.ExpectedTitle || !book.Authors.Any(a => a.Author.Name == e.ExpectedAuthor)))
                throw new InvalidOperationException($"Stored title/author changed for #{e.Number}.");
            var isbn10Prefix = e.Isbn13?.Substring(3, 9);
            var isbn10Check = isbn10Prefix == null ? 0 : (11 - isbn10Prefix.Select((c,i) => (c-'0') * (10-i)).Sum()%11)%11;
            var isbn10 = e.Isbn13?.StartsWith("978") == true ? isbn10Prefix + (isbn10Check == 10 ? "X" : isbn10Check.ToString()) : null;
            var expectedId = book?.Id;
            var sourceOwner = await db.ExternalReferences.SingleOrDefaultAsync(r => r.Source == CatalogSource.Manual
                && r.ExternalId == e.SourceUrl, ct);
            if (sourceOwner != null && sourceOwner.BookId != expectedId)
                throw new InvalidOperationException($"Source already belongs to another book: #{e.Number}");
            if (await db.Editions.AnyAsync(x => ((e.Isbn13 != null && x.Isbn13 == e.Isbn13) || (isbn10 != null && x.Isbn10 == isbn10))
                && (expectedId == null || x.BookId != expectedId), ct))
                throw new InvalidOperationException($"ISBN already belongs to another book: #{e.Number} {e.Isbn13}");
            if (book == null && await db.Books.AnyAsync(b => b.Title == e.Title
                && b.Authors.Any(a => a.Author.Name == e.Author), ct))
                throw new InvalidOperationException($"Explicit existing book mapping required for #{e.Number}.");
            var created = book == null;
            if (book == null)
            {
                book = new Book(e.Title, "und");
                var author = authors.FirstOrDefault(a => a.Name == e.Author);
                if (author == null) { author = new Author(e.Author); db.Authors.Add(author); authors.Add(author); }
                book.Authors.Add(new BookAuthor(book.Id, author.Id, 0));
                db.Books.Add(book);
            }
            book.UseVerifiedTurkishTitle(e.Title);
            if (sourceOwner == null)
            {
                var reference = ExternalReference.CreateForBook(book.Id, CatalogSource.Manual, e.SourceUrl, e.SourceUrl);
                book.ExternalReferences.Add(reference);
                db.ExternalReferences.Add(reference);
            }
            var publisher = publishers.FirstOrDefault(p => p.Name == e.Publisher);
            if (publisher == null) { publisher = new Publisher(e.Publisher); db.Publishers.Add(publisher); publishers.Add(publisher); }
            if (e.Isbn13 == null || !book.Editions.Any(x => x.Isbn13 == e.Isbn13))
            {
                var edition = new Edition(book.Id, e.EditionTitle, "tur", EditionFormat.Unknown,
                    publisher.Id, isbn13: e.Isbn13, pageCount: e.PageCount, coverImageUrl: e.CoverImageUrl);
                book.Editions.Add(edition);
                db.Editions.Add(edition);
            }
            if (!book.Categories.Any(c => c.CategoryId == classic.Id))
                book.Categories.Add(new Domain.Categories.BookCategory(book.Id, classic.Id));
            book.NovelReviews.Add(new NovelReview(book.Id, e.Decision, batchId, e.SourceUrl,
                $"{e.EvidenceNote} Source SHA256 {e.SourceSha256}; entry SHA256 {fingerprint}.",
                "reviewed-reading-list", "requested-classics-v1"));
            output.Add(new(e.Number, book.Id, e.Title, created, false));
        }
        if (apply)
        {
            await db.SaveChangesAsync(ct);
            var ids = output.Select(x => x.BookId).ToArray();
            if (await db.Books.EligibleNovels().CountAsync(b => ids.Contains(b.Id), ct) != output.Count)
                throw new InvalidOperationException("Not all requested entries are visible; rolling back.");
            await tx.CommitAsync(ct);
            return output.Select(x => x with { Visible = true }).ToArray();
        }
        await tx.RollbackAsync(ct);
        return output.ToArray();
    }

    public static void Validate(ReviewedReadingListEntry e)
    {
        if (e.Number < 1 || string.IsNullOrWhiteSpace(e.Title) || e.Title.Length > 500
            || string.IsNullOrWhiteSpace(e.Author) || e.Author.Length > 300
            || string.IsNullOrWhiteSpace(e.EditionTitle) || e.EditionTitle.Length > 500
            || string.IsNullOrWhiteSpace(e.Publisher) || e.Publisher.Length > 300
            || e.LanguageCode != "tur" || e.Decision is not (NovelDecision.Novel or NovelDecision.SelectedClassic)
            || string.IsNullOrWhiteSpace(e.EvidenceNote) || e.EvidenceNote.Length > 1400
            || e.PageCount is <= 0 || e.SourceSha256.Length != 64 || !e.SourceSha256.All(Uri.IsHexDigit)
            || (e.Isbn13 != null && (e.Isbn13.Length != 13 || !e.Isbn13.All(char.IsAsciiDigit)
            || !(e.Isbn13.StartsWith("978") || e.Isbn13.StartsWith("979"))
            || e.Isbn13.Select((c,i) => (c-'0') * (i%2 == 0 ? 1 : 3)).Sum()%10 != 0)))
            throw new ArgumentException($"Invalid reviewed entry #{e.Number}.");
        if (!Uri.TryCreate(e.SourceUrl, UriKind.Absolute, out var source) || source.Scheme != "https"
            || !Uri.TryCreate(e.CoverImageUrl, UriKind.Absolute, out var cover) || cover.Scheme != "https"
            || !source.IsDefaultPort || !cover.IsDefaultPort
            || source.UserInfo != "" || cover.UserInfo != "") throw new ArgumentException("Unapproved evidence/cover host.");
        var modern = source.Host == "www.kitapyurdu.com" && source.AbsolutePath.StartsWith("/kitap/")
            && cover.Host == "img.kitapyurdu.com" && e.Isbn13 != null;
        var vintage = source.Host == "www.nadirkitap.com" && source.AbsolutePath.EndsWith(".html")
            && cover.Host == "static.nadirkitap.com"
            && (e.Isbn13 != null || e.EvidenceNote.Contains("ISBN not assigned")
                || e.EvidenceNote.Contains("ISBN unavailable in source"));
        if (!modern && !vintage) throw new ArgumentException("Unapproved edition evidence.");
        if (e.ExistingBookId != null && (string.IsNullOrWhiteSpace(e.ExpectedTitle) || string.IsNullOrWhiteSpace(e.ExpectedAuthor)))
            throw new ArgumentException("Existing book mapping requires exact title and author preconditions.");
    }
}
