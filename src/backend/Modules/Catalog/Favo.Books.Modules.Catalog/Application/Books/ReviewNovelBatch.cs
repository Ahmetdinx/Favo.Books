using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Application.Books;

public sealed record NovelReviewEntry(string WorkId, string ExpectedTitle, string ExpectedAuthor,
    NovelDecision Decision, string SourceUrl, string EvidenceNote);
public sealed record NovelReviewBatch(string BatchId, string Reviewer, string PolicyVersion, NovelReviewEntry[] Entries);
public sealed record NovelReviewBatchResult(int Matched, int Added, bool Applied);

/// <summary>Offline editorial operation. Never exposed through an unauthorised HTTP endpoint.</summary>
public sealed class ReviewNovelBatch(CatalogDbContext db)
{
    public async Task<NovelReviewBatchResult> ExecuteAsync(NovelReviewBatch batch, bool apply,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Entries is null || batch.Entries.Length == 0 || batch.Entries.Length > 1000)
            throw new ArgumentException("Provide between 1 and 1000 reviewed entries.");
        if (batch.Entries.Any(e => e is null) || batch.Entries.Select(e => e.WorkId).Distinct().Count() != batch.Entries.Length)
            throw new ArgumentException("Duplicate or missing work entries.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize editorial batches so append order also defines latest decision order.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(842619002)", ct);
        var pending = new List<NovelReview>();
        var matched = new HashSet<Guid>();
        foreach (var entry in batch.Entries)
        {
            var book = await db.Books.Include(b => b.Authors).ThenInclude(a => a.Author)
                .Include(b => b.Editions).AsSplitQuery()
                .SingleOrDefaultAsync(b => b.ExternalReferences.Any(r =>
                    r.Source == Domain.Sources.CatalogSource.OpenLibrary && r.ExternalId == entry.WorkId), ct)
                ?? throw new InvalidOperationException($"Work not found: {entry.WorkId}");
            if (!matched.Add(book.Id)) throw new InvalidOperationException("Multiple entries identify the same book.");
            if (book.Title != entry.ExpectedTitle || !book.Authors.Any(a => a.Author.Name == entry.ExpectedAuthor))
                throw new InvalidOperationException($"Title/author mismatch: {entry.WorkId}; stored title: {book.Title}; stored authors: {string.Join(", ", book.Authors.Select(a => a.Author.Name))}");
            if (entry.Decision is NovelDecision.Novel or NovelDecision.SelectedClassic && !book.Editions.Any(e =>
                e.LanguageCode is "tr" or "tur" && !string.IsNullOrWhiteSpace(e.CoverImageUrl)))
                throw new InvalidOperationException($"Covered Turkish edition missing: {entry.WorkId}");
            var review = new NovelReview(book.Id, entry.Decision, batch.BatchId, entry.SourceUrl,
                entry.EvidenceNote, batch.Reviewer, batch.PolicyVersion);
            var existing = await db.NovelReviews.AsNoTracking().SingleOrDefaultAsync(r =>
                r.BookId == book.Id && r.BatchId == review.BatchId, ct);
            if (existing is not null)
            {
                if (existing.Decision != review.Decision || existing.SourceUrl != review.SourceUrl
                    || existing.EvidenceNote != review.EvidenceNote || existing.Reviewer != review.Reviewer
                    || existing.PolicyVersion != review.PolicyVersion)
                    throw new InvalidOperationException("Existing batch differs; use a new batch ID to append a correction.");
                continue;
            }
            pending.Add(review);
        }
        if (apply)
        {
            db.NovelReviews.AddRange(pending);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        else await transaction.RollbackAsync(ct);
        return new(matched.Count, apply ? pending.Count : 0, apply);
    }
}
