using Favo.Books.Modules.Profiles.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Favo.Books.Modules.Profiles.Application;

public sealed record PublicInfluentialBook(Guid BookId, int Position);
public sealed record PublicShowcaseResult(Guid? BedsideBookId, PublicInfluentialBook[] InfluentialBooks);
public sealed class PublicShowcase(ProfilesDbContext db)
{
    public async Task<PublicShowcaseResult> Get(Guid id, CancellationToken ct)
    {
        var bedside = await db.BedsideBooks.AsNoTracking().Where(b => b.UserId == id).Select(b => (Guid?)b.BookId).SingleOrDefaultAsync(ct);
        var influential = await db.InfluentialBooks.AsNoTracking().Where(b => b.UserId == id).OrderBy(b => b.Position)
            .Select(b => new PublicInfluentialBook(b.BookId, b.Position)).ToArrayAsync(ct);
        // PersonalNote and PersonalReflection intentionally stay private.
        return new(bedside, influential);
    }
}
