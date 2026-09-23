using KitapKivo.Modules.Community.Domain.Ratings;
using KitapKivo.Modules.Community.Persistence;
using KitapKivo.SharedKernel.Catalog;
using KitapKivo.SharedKernel.Reading;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Community.Application.Ratings;

internal sealed record RecommendedBook(Guid BookId, string Title, string CoverImageUrl, string[] Reasons, int RatingCount);
internal sealed record FeedbackItem(Guid BookId, string Title, string Choice);
internal sealed record RecommendationResult(RecommendedBook[] Items, bool HasPersonalSignals, FeedbackItem[] Feedback);
internal sealed record PreferenceRequest(string[]? Categories, string[]? Criteria);

internal sealed class BookRecommendations(CommunityDbContext db, IDiscoveryCatalog catalog, IReadingHistory reading)
{
    internal static readonly IReadOnlyDictionary<string, string> CriteriaNames = new Dictionary<string, string>
    {
        ["theme"] = "Konu / Fikir", ["language"] = "Anlatım ve Dil", ["characters"] = "Karakterler",
        ["structure"] = "Kurgu ve Yapı", ["pace"] = "Tempo / Sürükleyicilik",
        ["impact"] = "Duygusal / Düşünsel Etki", ["experience"] = "Genel Okuma Deneyimi"
    };

    public async Task<ReadingPreference?> GetPreferencesAsync(Guid userId, CancellationToken ct)
    {
        var preference = await db.ReadingPreferences.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, ct);
        if (preference is not null)
        {
            var genres = (await catalog.GetGenresAsync(ct)).Select(c => c.Slug).ToHashSet();
            preference.Categories = preference.Categories.Where(genres.Contains).ToArray();
        }
        return preference;
    }

    public async Task<bool> SavePreferencesAsync(Guid userId, PreferenceRequest request, CancellationToken ct)
    {
        if (request.Categories is null || request.Criteria is null || request.Categories.Length > 5
            || request.Criteria.Length > 3 || request.Categories.Distinct().Count() != request.Categories.Length
            || request.Criteria.Distinct().Count() != request.Criteria.Length
            || request.Criteria.Any(c => c is null || !CriteriaNames.ContainsKey(c))) return false;
        var knownCategories = (await catalog.GetGenresAsync(ct)).Select(c => c.Slug).ToHashSet();
        if (request.Categories.Any(c => c is null || !knownCategories.Contains(c))) return false;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO community."ReadingPreferences" ("UserId", "Categories", "Criteria")
            VALUES ({userId}, {request.Categories}, {request.Criteria})
            ON CONFLICT ("UserId") DO UPDATE SET "Categories" = EXCLUDED."Categories", "Criteria" = EXCLUDED."Criteria"
            """, ct);
        return true;
    }

    public async Task<RecommendationResult> GetAsync(Guid userId, CancellationToken ct)
    {
        var preference = await GetPreferencesAsync(userId, ct) ?? new ReadingPreference { UserId = userId };
        var books = await catalog.GetBooksAsync(ct);
        var started = (await reading.GetStartedBookIdsAsync(userId, ct)).ToHashSet();
        var ratings = await db.BookRatings.AsNoTracking().Where(r => r.OverallScore != null).ToArrayAsync(ct);
        var feedback = await db.RecommendationFeedback.AsNoTracking().Where(f => f.UserId == userId).ToArrayAsync(ct);
        return Rank(books, ratings, started, userId, preference, feedback);
    }

    public async Task<bool> SetFeedbackAsync(Guid userId, Guid bookId, string choice, CancellationToken ct)
    {
        if (choice is not ("interested" or "dismissed")) throw new ArgumentOutOfRangeException(nameof(choice));
        if (!(await catalog.GetBooksAsync(ct)).Any(b => b.BookId == bookId)) return false;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO community."RecommendationFeedback" ("UserId", "BookId", "Choice")
            VALUES ({userId}, {bookId}, {choice})
            ON CONFLICT ("UserId", "BookId") DO UPDATE SET "Choice" = EXCLUDED."Choice"
            """, ct);
        return true;
    }

    public Task<int> ClearFeedbackAsync(Guid userId, Guid bookId, CancellationToken ct)
        => db.RecommendationFeedback.Where(f => f.UserId == userId && f.BookId == bookId).ExecuteDeleteAsync(ct);

    internal static RecommendationResult Rank(IReadOnlyList<DiscoveryBook> books, IReadOnlyList<BookRating> ratings,
        HashSet<Guid> started, Guid userId, ReadingPreference preference, IReadOnlyList<RecommendationFeedback>? feedback = null)
    {
        var own = ratings.Where(r => r.UserId == userId).ToArray();
        var excluded = started.Concat(own.Select(r => r.BookId)).ToHashSet();
        var ownFeedback = (feedback ?? []).Where(f => f.UserId == userId).ToArray();
        excluded.UnionWith(ownFeedback.Where(f => f.Choice == "dismissed").Select(f => f.BookId));
        var interestedIds = ownFeedback.Where(f => f.Choice == "interested").Select(f => f.BookId).ToHashSet();
        var interested = books.Where(b => interestedIds.Contains(b.BookId)).OrderBy(b => b.BookId).ToArray();
        var likedIds = own.Where(r => r.OverallScore >= 7).Select(r => r.BookId).ToHashSet();
        var liked = books.Where(b => likedIds.Contains(b.BookId)).OrderBy(b => b.BookId).ToArray();
        var byBook = ratings.GroupBy(r => r.BookId).ToDictionary(g => g.Key, g => g.ToArray());
        var candidates = new List<(RecommendedBook Book, decimal Score)>();
        foreach (var book in books.Where(b => !excluded.Contains(b.BookId)))
        {
            List<string> reasons = [];
            decimal score = 0;
            var interestMatch = interested.FirstOrDefault(b => b.BookId != book.BookId &&
                (b.AuthorIds.Intersect(book.AuthorIds).Any() || b.Categories.Any(c => book.Categories.Any(x => x.Slug == c.Slug))));
            if (interestMatch is not null)
            {
                score += 1;
                reasons.Add($"İlgini çeken «{interestMatch.Title}» ile ortak türü veya yazarı var.");
            }
            var selectedCategory = book.Categories.FirstOrDefault(c => preference.Categories.Contains(c.Slug));
            if (selectedCategory is not null)
            {
                score += 4;
                reasons.Add($"Seçtiğin {selectedCategory.Name} türünde.");
            }
            var similar = liked.FirstOrDefault(b => b.Categories.Any(c => book.Categories.Any(x => x.Slug == c.Slug)));
            if (similar is not null)
            {
                score += 2;
                reasons.Add($"Beğendiğin «{similar.Title}» ile ortak bir türü var.");
            }
            var sameAuthor = liked.FirstOrDefault(b => b.AuthorIds.Intersect(book.AuthorIds).Any());
            if (sameAuthor is not null)
            {
                score += 3;
                reasons.Add($"Beğendiğin «{sameAuthor.Title}» ile aynı yazarı paylaşıyor.");
            }
            var votes = byBook.GetValueOrDefault(book.BookId) ?? [];
            // Five neutral prior votes reduce the influence of tiny samples. No match percentage is claimed.
            decimal quality = (votes.Sum(v => v.OverallScore!.Value) + 25) / (votes.Length + 5);
            score += quality / 10;
            foreach (var criterion in preference.Criteria)
            {
                if (votes.Length < 3) continue;
                decimal average = votes.Average(v => CriterionScore(v, criterion));
                decimal adjusted = (average * votes.Length + 25) / (votes.Length + 5);
                score += (adjusted - 5) / preference.Criteria.Length;
                if (average >= 7)
                    reasons.Add($"Önem verdiğin {CriteriaNames[criterion]}: {average.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"))}/10 ({votes.Length} değerlendirme).");
            }
            if (reasons.Count == 0) reasons.Add("Keşif önerisi: kişisel eşleşme için henüz yeterli veri yok.");
            candidates.Add((new(book.BookId, book.Title, book.CoverImageUrl, reasons.Take(3).ToArray(), votes.Length), score));
        }
        return new(candidates.OrderByDescending(c => c.Score).ThenBy(c => c.Book.BookId)
            .Take(6).Select(c => c.Book).ToArray(),
            preference.Categories.Length > 0 || preference.Criteria.Length > 0 || liked.Length > 0 || interested.Length > 0,
            ownFeedback.OrderBy(f => f.BookId).Select(f => new FeedbackItem(f.BookId,
                books.FirstOrDefault(b => b.BookId == f.BookId)?.Title ?? "Katalogda bulunmayan kitap", f.Choice)).ToArray());
    }

    private static decimal CriterionScore(BookRating r, string criterion) => criterion switch
    {
        "theme" => r.Theme!.Value, "language" => r.Language!.Value, "characters" => r.Characters!.Value,
        "structure" => r.Structure!.Value, "pace" => r.Pace!.Value, "impact" => r.Impact!.Value,
        "experience" => r.Experience!.Value, _ => throw new ArgumentOutOfRangeException(nameof(criterion))
    };
}
