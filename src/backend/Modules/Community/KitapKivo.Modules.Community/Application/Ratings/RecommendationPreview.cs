using KitapKivo.Modules.Community.Domain.Ratings;
using KitapKivo.SharedKernel.Catalog;

namespace KitapKivo.Modules.Community.Application.Ratings;

// In-memory fixtures only: the preview uses the real ranking algorithm, never database writes.
internal static class RecommendationPreview
{
    internal static RecommendationResult Create(Guid[] excluded)
    {
        var user = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var books = Enumerable.Range(1, 16).Select(i => new DiscoveryBook(
            new Guid(i, 0, 0, new byte[8]),
            i == 15 ? "Sınır testi · Tempo 9" : i == 16 ? "Az veri · 2 değerlendirme" : $"Örnek roman {i}",
            "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='120' height='180'%3E%3Crect width='120' height='180' fill='%2379563d'/%3E%3Ctext x='60' y='95' text-anchor='middle' fill='%23f1e8dc' font-size='18'%3EDEMO%3C/text%3E%3C/svg%3E",
            [], [new DiscoveryCategory(i >= 9 && i <= 12 ? "fantastik" : "klasikler", i >= 9 && i <= 12 ? "Fantastik" : "Klasikler")])).ToArray();
        var ratings = books.SelectMany((book, index) => Enumerable.Range(0, index == 15 ? 2 : 4).Select(v =>
            BookRating.Create(new Guid(v + 100, 0, 0, new byte[8]), book.BookId,
                new RatingScores(8, 10, 10, 8, index == 14 ? 9 : index >= 12 && index < 14 ? 7 : 10, 8, 8),
                DateTimeOffset.UtcNow))).ToArray();
        return BookRecommendations.Rank(books, ratings, excluded.ToHashSet(), user,
            new ReadingPreference { Categories = ["klasikler"], Criteria = ["language", "characters", "pace"] });
    }
}
