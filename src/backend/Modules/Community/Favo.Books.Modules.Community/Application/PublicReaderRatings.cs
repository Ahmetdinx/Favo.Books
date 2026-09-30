using Favo.Books.Modules.Community.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Favo.Books.Modules.Community.Application;

public sealed record PublicReaderRating(Guid BookId, int? Theme, int? Language, int? Characters, int? Structure,
    int? Pace, int? Impact, int? Experience, decimal? OverallScore);
public sealed class PublicReaderRatings(CommunityDbContext db, Favo.Books.SharedKernel.Catalog.INovelAchievementCatalog catalog)
{
    public async Task<int> CountEvaluatedNovels(Guid id, CancellationToken ct)
    {
        var ids = await db.BookRatings.AsNoTracking().Where(r => r.UserId == id && r.OverallScore != null
            && r.Theme != null && r.Language != null && r.Characters != null && r.Structure != null
            && r.Pace != null && r.Impact != null && r.Experience != null)
            .Select(r => r.BookId).Distinct().ToArrayAsync(ct);
        return await catalog.CountEligibleAsync(ids, ct);
    }

    public Task<PublicReaderRating[]> Get(Guid id, Guid[] bookIds, CancellationToken ct) =>
        db.BookRatings.AsNoTracking().Where(r => r.UserId == id && bookIds.Contains(r.BookId))
            .Select(r => new PublicReaderRating(r.BookId, r.Theme, r.Language, r.Characters, r.Structure,
                r.Pace, r.Impact, r.Experience, r.OverallScore)).ToArrayAsync(ct);
}
