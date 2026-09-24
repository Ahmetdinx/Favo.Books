using KitapKivo.Modules.Reading.Persistence;
using Microsoft.EntityFrameworkCore;
namespace KitapKivo.Modules.Reading.Application;

public sealed record PublicLibraryItem(Guid BookId, string Status);
public sealed record PublicLibraryPage(PublicLibraryItem[] Items, int TotalCount);
public sealed class PublicLibrary(ReadingDbContext db)
{
    public async Task<PublicLibraryPage> Get(Guid id, int page, CancellationToken ct)
    {
        var query = db.LibraryEntries.AsNoTracking().Where(e => e.UserId == id);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(e => e.UpdatedAtUtc).ThenBy(e => e.BookId)
            .Skip((page - 1) * 20).Take(20).Select(e => new { e.BookId, e.Status }).ToArrayAsync(ct);
        return new(items.Select(e => new PublicLibraryItem(e.BookId, e.Status.ToString())).ToArray(), count);
    }
}
