using KitapKivo.Modules.Reading.Domain.LibraryEntries;
using KitapKivo.Modules.Reading.Persistence;
using KitapKivo.SharedKernel.Reading;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Reading.Application.LibraryEntries;

internal sealed class CompletedBooks(ReadingDbContext dbContext) : ICompletedBooks
{
    public async Task<IReadOnlyList<Guid>> GetBookIdsAsync(Guid userId, int offset, int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        return await dbContext.LibraryEntries.AsNoTracking()
            .Where(entry => entry.UserId == userId && entry.Status == LibraryEntryStatus.Read)
            .OrderByDescending(entry => entry.UpdatedAtUtc).ThenBy(entry => entry.Id)
            .Skip(offset).Take(limit).Select(entry => entry.BookId)
            .ToArrayAsync(cancellationToken);
    }
}
