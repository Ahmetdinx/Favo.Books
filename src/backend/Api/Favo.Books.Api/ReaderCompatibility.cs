using Favo.Books.Modules.Community.Application;
using Favo.Books.Modules.Profiles.Application;
using Favo.Books.Modules.Reading.Application;
using Favo.Books.SharedKernel.Catalog;
namespace Favo.Books.Api;

public sealed record CompatibilitySuggestion(Guid BookId, decimal? Score, bool Influential);
public sealed record ReaderCompatibilityResult(bool Ready, int? Percent, string? Label, string[] SharedGenres,
    int CommonBooks, int ComparedRatings, CompatibilitySuggestion[] Suggestions);

/// <summary>Taste side of two readers: what both read and like, and the books one loved that the other has not met yet.</summary>
public sealed record TasteSide(Guid[] Read, PublicReaderRating[] Rated);

/// <summary>
/// How close two readers' tastes are. Genre leanings always count; once both readers rated the same novels,
/// their score agreement weighs in too (up to half, reached at five shared ratings).
/// </summary>
public sealed class ReaderCompatibility(PublicLibrary library, PublicReaderRatings ratings, PublicShowcase showcase, IDiscoveryCatalog catalog)
{
    const int SuggestionCount = 3;
    const decimal LovedScore = 8m;

    public async Task<ReaderCompatibilityResult> Get(Guid me, Guid other, CancellationToken ct)
    {
        // Sequential: the module readers may share scoped contexts.
        var mine = new TasteSide(await library.ReadBookIds(me, ct), await ratings.Complete(me, ct));
        var theirs = new TasteSide(await library.ReadBookIds(other, ct), await ratings.Complete(other, ct));
        var influential = (await showcase.Get(other, ct)).InfluentialBooks.OrderBy(b => b.Position).Select(b => b.BookId).ToArray();
        var ids = mine.Read.Concat(mine.Rated.Select(r => r.BookId)).Concat(theirs.Read).Concat(theirs.Rated.Select(r => r.BookId))
            .Concat(influential).Distinct().ToArray();
        var genres = await catalog.GetGenresForBooksAsync(ids, ct);

        // Anything already on my shelves or rated by me (even partially) is not a suggestion.
        var met = (await library.AllBookIds(me, ct)).ToHashSet();
        var candidates = theirs.Rated.Select(r => r.BookId).Concat(influential).Where(id => !met.Contains(id)).Distinct().ToArray();
        if (candidates.Length > 0) met.UnionWith((await ratings.Get(me, candidates, ct)).Select(r => r.BookId));

        return Build(Visible(mine, genres), Visible(theirs, genres), influential.Where(genres.ContainsKey).ToArray(), met, genres);
    }

    static TasteSide Visible(TasteSide side, IReadOnlyDictionary<Guid, DiscoveryCategory[]> genres) =>
        new(side.Read.Where(genres.ContainsKey).ToArray(), side.Rated.Where(r => genres.ContainsKey(r.BookId)).ToArray());

    internal static ReaderCompatibilityResult Build(TasteSide mine, TasteSide theirs, Guid[] theirInfluential,
        IReadOnlySet<Guid> alreadyMet, IReadOnlyDictionary<Guid, DiscoveryCategory[]> genres)
    {
        if (!IsReady(mine) || !IsReady(theirs)) return new(false, null, null, [], 0, 0, []);

        var myGenres = ReaderTaste.GenreWeights(mine.Read, mine.Rated, genres);
        var theirGenres = ReaderTaste.GenreWeights(theirs.Read, theirs.Rated, genres);
        // A small floor: two readers of novels always share something, even with no genre in common.
        var genreSimilarity = 0.1 + 0.9 * Cosine(myGenres.ToDictionary(g => g.Key, g => g.Value.Score), theirGenres.ToDictionary(g => g.Key, g => g.Value.Score));

        var theirScores = theirs.Rated.Where(r => r.OverallScore != null).ToDictionary(r => r.BookId, r => r.OverallScore!.Value);
        var pairs = mine.Rated.Where(r => r.OverallScore != null && theirScores.ContainsKey(r.BookId))
            .Select(r => (Mine: r.OverallScore!.Value, Theirs: theirScores[r.BookId])).ToArray();
        var similarity = genreSimilarity;
        if (pairs.Length > 0)
        {
            // 1 when scores match, 0 when one gave 1 and the other 10.
            var agreement = 1 - (double)pairs.Average(p => Math.Abs(p.Mine - p.Theirs)) / 9;
            var weight = Math.Min(pairs.Length, 5) / 10.0;
            similarity = (1 - weight) * genreSimilarity + weight * agreement;
        }
        var percent = (int)Math.Round(Math.Clamp(similarity, 0, 1) * 100);

        var myBooks = mine.Read.Concat(mine.Rated.Select(r => r.BookId)).ToHashSet();
        var common = theirs.Read.Concat(theirs.Rated.Select(r => r.BookId)).Distinct().Count(myBooks.Contains);

        // Genres both lean on, ordered by the smaller of the two shares.
        var myTotal = myGenres.Values.Sum(g => g.Score);
        var theirTotal = theirGenres.Values.Sum(g => g.Score);
        var shared = myGenres.Where(g => theirGenres.ContainsKey(g.Key))
            .Select(g => (g.Value.Name, Share: Math.Min(g.Value.Score / myTotal, theirGenres[g.Key].Score / theirTotal)))
            .Where(g => g.Share >= 0.1m).OrderByDescending(g => g.Share).ThenBy(g => g.Name).Take(3).Select(g => g.Name).ToArray();

        var influentialRank = theirInfluential.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var suggestions = theirScores.Where(s => s.Value >= LovedScore).Select(s => s.Key).Concat(theirInfluential)
            .Distinct().Where(id => !alreadyMet.Contains(id) && !myBooks.Contains(id))
            .Select(id => new CompatibilitySuggestion(id, theirScores.TryGetValue(id, out var score) ? score : null, influentialRank.ContainsKey(id)))
            .OrderByDescending(s => s.Influential).ThenBy(s => influentialRank.GetValueOrDefault(s.BookId))
            .ThenByDescending(s => s.Score).ThenBy(s => s.BookId).Take(SuggestionCount).ToArray();

        return new(true, percent, Label(percent), shared, common, pairs.Length, suggestions);
    }

    static bool IsReady(TasteSide side) =>
        side.Rated.Length >= ReaderTaste.MinimumRatings ||
        side.Read.Concat(side.Rated.Select(r => r.BookId)).Distinct().Count() >= ReaderTaste.MinimumBooks;

    static double Cosine(Dictionary<string, decimal> a, Dictionary<string, decimal> b)
    {
        var dot = a.Where(x => b.ContainsKey(x.Key)).Sum(x => (double)(x.Value * b[x.Key]));
        var norm = Math.Sqrt(a.Values.Sum(v => (double)(v * v))) * Math.Sqrt(b.Values.Sum(v => (double)(v * v)));
        return norm == 0 ? 0 : dot / norm;
    }

    static string Label(int percent) => percent switch
    {
        >= 85 => "Kitap ikizi",
        >= 70 => "Çok uyumlu",
        >= 50 => "Ortak raflar",
        >= 30 => "Farklı raflar",
        _ => "Farklı dünyalar"
    };
}
