using KitapKivo.Modules.Community.Persistence;
using Microsoft.EntityFrameworkCore;
namespace KitapKivo.Modules.Community.Application;

public sealed record PublicReaderRating(Guid BookId, int? Theme, int? Language, int? Characters, int? Structure,
    int? Pace, int? Impact, int? Experience, decimal? OverallScore);
public sealed class PublicReaderRatings(CommunityDbContext db)
{
    public Task<PublicReaderRating[]> Get(Guid id, Guid[] bookIds, CancellationToken ct) =>
        db.BookRatings.AsNoTracking().Where(r => r.UserId == id && bookIds.Contains(r.BookId))
            .Select(r => new PublicReaderRating(r.BookId, r.Theme, r.Language, r.Characters, r.Structure,
                r.Pace, r.Impact, r.Experience, r.OverallScore)).ToArrayAsync(ct);
}
