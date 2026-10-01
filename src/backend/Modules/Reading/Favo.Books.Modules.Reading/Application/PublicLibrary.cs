using Favo.Books.Modules.Reading.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Favo.Books.Modules.Reading.Application;

public sealed record PublicLibraryItem(Guid BookId, string Status);
public sealed record PublicLibraryPage(PublicLibraryItem[] Items, int TotalCount);
public sealed class PublicLibrary(ReadingDbContext db, Favo.Books.SharedKernel.Catalog.INovelAchievementCatalog catalog)
{
    public sealed record ReadingActivity(Guid UserId, Guid BookId, DateTimeOffset UpdatedAtUtc);
    public Task<ReadingActivity[]> CurrentReading(Guid[] readers, CancellationToken ct) =>
        db.LibraryEntries.AsNoTracking().Where(e => readers.Contains(e.UserId) && e.Status == Domain.LibraryEntries.LibraryEntryStatus.Reading)
            .OrderByDescending(e => e.UpdatedAtUtc).ThenBy(e => e.UserId).ThenBy(e => e.BookId).Take(12)
            .Select(e => new ReadingActivity(e.UserId, e.BookId, e.UpdatedAtUtc)).ToArrayAsync(ct);

    public async Task<int> CountCompletedNovels(Guid id, CancellationToken ct)
    {
        var ids = await db.LibraryEntries.AsNoTracking()
            .Where(e => e.UserId == id && e.Status == Domain.LibraryEntries.LibraryEntryStatus.Read)
            .Select(e => e.BookId).Distinct().ToArrayAsync(ct);
        return await catalog.CountEligibleAsync(ids, ct);
    }

    public async Task<PublicLibraryPage> Get(Guid id, int page, CancellationToken ct)
    {
        var query = db.LibraryEntries.AsNoTracking().Where(e => e.UserId == id);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.UpdatedAtUtc).ThenBy(e => e.BookId)
            .Skip((page - 1) * 20).Take(20).Select(e => new { e.BookId, e.Status }).ToArrayAsync(ct);
        return new(items.Select(e => new PublicLibraryItem(e.BookId, e.Status.ToString())).ToArray(), count);
    }
}
