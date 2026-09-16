using KitapKivo.Modules.Reading.Domain.LibraryEntries;
using KitapKivo.Modules.Reading.Persistence;
using KitapKivo.SharedKernel.Reading;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Reading.Application.LibraryEntries;

internal sealed class ReadingHistory(ReadingDbContext db) : IReadingHistory
{
    public async Task<IReadOnlyList<Guid>> GetStartedBookIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await db.LibraryEntries.AsNoTracking()
            .Where(e => e.UserId == userId && (e.Status == LibraryEntryStatus.Read || e.Status == LibraryEntryStatus.Reading))
            .Select(e => e.BookId).ToArrayAsync(cancellationToken);
}
