using KitapKivo.Modules.Catalog.Domain.Books;

namespace KitapKivo.Modules.Catalog.Application.Books;

internal static class NovelEligibility
{
    // Unknown and unreviewed books remain stored, but are excluded from discovery.
    public static IQueryable<Book> EligibleNovels(this IQueryable<Book> books)
        => books.Where(b => (b.NovelReviews.OrderByDescending(r => r.Id)
            .Select(r => r.Decision).FirstOrDefault() == NovelDecision.Novel
            || b.NovelReviews.OrderByDescending(r => r.Id)
                .Select(r => r.Decision).FirstOrDefault() == NovelDecision.SelectedClassic)
            && b.Editions.Any(e => (e.LanguageCode == "tr" || e.LanguageCode == "tur")
                && e.CoverImageUrl != null && e.CoverImageUrl.Trim() != ""));
}
