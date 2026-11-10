using Favo.Books.Modules.Community.Application;
using Favo.Books.Modules.Reading.Application;
using Favo.Books.SharedKernel.Catalog;
namespace Favo.Books.Api;

public sealed record TasteGenre(string Slug, string Name, int Percent);
public sealed record TasteCriterion(string Key, string Label);
public sealed record ReaderTasteProfile(bool Ready, int ReadCount, int RatingCount, int BooksNeeded, int RatingsNeeded,
    string? Title, TasteGenre[] Genres, TasteCriterion[] Strengths, string[] Traits, decimal? AverageScore);

/// <summary>
/// A reader's taste, derived only from their own library and complete seven-criterion ratings.
/// Genres weigh read books equally and rated books by how much the reader liked them; strengths are the
/// criteria that stand out above the reader's own average in the books they liked most.
/// </summary>
public sealed class ReaderTaste(PublicLibrary library, PublicReaderRatings ratings, IDiscoveryCatalog catalog)
{
    public const int MinimumBooks = 5;
    public const int MinimumRatings = 3;
    const string Classics = "klasikler";

    static readonly (string Key, string Label)[] Criteria =
    [
        ("theme", "Konu ve fikir"), ("language", "Dil ve anlatım"), ("characters", "Karakterler"),
        ("structure", "Kurgu"), ("pace", "Tempo"), ("impact", "Bıraktığı iz"), ("experience", "Okuma keyfi")
    ];

    static readonly Dictionary<string, string> Archetypes = new()
    {
        ["polisiye-ve-gizem"] = "Dedektif ruhlu okur", ["gerilim"] = "Gerilim avcısı", ["korku"] = "Karanlığı seven okur",
        ["bilim-kurgu"] = "Gelecek meraklısı", ["fantastik"] = "Hayal gezgini", ["distopya"] = "Distopya kâşifi",
        ["tarihi-roman"] = "Zaman yolcusu", ["savas"] = "Cephe tanığı", ["ask-ve-romantik"] = "Romantik ruh",
        ["psikolojik-roman"] = "İç dünyaların okuru", ["felsefi-roman"] = "Soru soran okur",
        ["toplumsal-roman"] = "Hayatın tanığı", ["macera"] = "Serüven tutkunu", ["mizah"] = "Gülümseyen okur",
        ["genclik"] = "Genç ruhlu okur"
    };

    public async Task<ReaderTasteProfile> Get(Guid userId, CancellationToken ct)
    {
        // Sequential: the library and rating readers may share scoped contexts with the catalog.
        var read = await library.ReadBookIds(userId, ct);
        var rated = await ratings.Complete(userId, ct);
        var bookIds = read.Concat(rated.Select(r => r.BookId)).Distinct().ToArray();
        var genres = await catalog.GetGenresForBooksAsync(bookIds, ct);
        return Build(read.Where(genres.ContainsKey).ToArray(), rated.Where(r => genres.ContainsKey(r.BookId)).ToArray(), genres);
    }

    internal static ReaderTasteProfile Build(Guid[] read, PublicReaderRating[] rated,
        IReadOnlyDictionary<Guid, DiscoveryCategory[]> genres)
    {
        var books = read.Concat(rated.Select(r => r.BookId)).Distinct().ToArray();
        var booksNeeded = Math.Max(0, MinimumBooks - books.Length);
        var ratingsNeeded = Math.Max(0, MinimumRatings - rated.Length);
        if (booksNeeded > 0 && ratingsNeeded > 0)
            return new(false, read.Length, rated.Length, booksNeeded, ratingsNeeded, null, [], [], [], null);

        var scores = GenreWeights(read, rated, genres);
        var total = scores.Values.Sum(s => s.Score);
        var topGenres = total == 0 ? [] : scores.OrderByDescending(s => s.Value.Score).ThenBy(s => s.Key).Take(3)
            .Select(s => new TasteGenre(s.Key, s.Value.Name, (int)Math.Round(s.Value.Score / total * 100))).ToArray();

        var strengths = rated.Length >= MinimumRatings ? Strengths(rated) : [];
        var traits = new List<string>();
        var withGenre = books.Where(id => genres[id].Length > 0).ToArray();
        if (withGenre.Length > 0 && withGenre.Count(id => genres[id].Any(g => g.Slug == Classics)) * 2 >= withGenre.Length)
            traits.Add("Klasiklere sadık");
        decimal? average = rated.Length > 0 ? Math.Round(rated.Average(r => r.OverallScore ?? 0), 1) : null;
        if (rated.Length >= MinimumRatings)
            traits.Add(average >= 8.2m ? "Cömert puanlayıcı" : average <= 6.2m ? "Titiz puanlayıcı" : "Dengeli puanlayıcı");
        if (topGenres.Length >= 3 && topGenres[0].Percent < 35) traits.Add("Türler arasında gezen");

        var title = topGenres.Length > 0 ? Archetypes.GetValueOrDefault(topGenres[0].Slug, "Meraklı okur") : "Meraklı okur";
        return new(true, read.Length, rated.Length, booksNeeded, ratingsNeeded, title, topGenres, strengths, traits.ToArray(), average);
    }

    /// <summary>Genre weights without the classics mark: read books count once, rated books 0.2 (1/10) to 2 (10/10).</summary>
    internal static Dictionary<string, (string Name, decimal Score)> GenreWeights(Guid[] read, PublicReaderRating[] rated,
        IReadOnlyDictionary<Guid, DiscoveryCategory[]> genres)
    {
        var weight = read.Concat(rated.Select(r => r.BookId)).Distinct().ToDictionary(id => id, _ => 1m);
        foreach (var r in rated) weight[r.BookId] = 0.2m + 1.8m * ((r.OverallScore ?? 5) - 1) / 9;
        var scores = new Dictionary<string, (string Name, decimal Score)>();
        foreach (var (id, w) in weight)
            foreach (var g in genres[id].Where(g => g.Slug != Classics))
                scores[g.Slug] = (g.Name, (scores.TryGetValue(g.Slug, out var s) ? s.Score : 0) + w);
        return scores;
    }

    /// <summary>Criteria scored clearly above the book's own average, in the books the reader liked at least as much as their median.</summary>
    static TasteCriterion[] Strengths(PublicReaderRating[] rated)
    {
        var median = rated.Select(r => r.OverallScore ?? 0).Order().ElementAt(rated.Length / 2);
        var liked = rated.Where(r => (r.OverallScore ?? 0) >= median).ToArray();
        var lifts = Criteria.Select((c, i) => (c.Key, c.Label, Lift: liked.Average(r =>
        {
            var values = Values(r);
            return values[i] - values.Average();
        }))).OrderByDescending(x => x.Lift).ToArray();
        return lifts.Where(x => x.Lift >= 0.3).Take(2).Select(x => new TasteCriterion(x.Key, x.Label)).ToArray();
    }

    static double[] Values(PublicReaderRating r) =>
        [r.Theme ?? 0, r.Language ?? 0, r.Characters ?? 0, r.Structure ?? 0, r.Pace ?? 0, r.Impact ?? 0, r.Experience ?? 0];
}
