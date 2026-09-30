using Favo.Books.Modules.Reading.Domain.LibraryEntries;
using Favo.Books.Modules.Reading.Persistence;
using Favo.Books.SharedKernel.Reading;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Reading.Application.LibraryEntries;

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
