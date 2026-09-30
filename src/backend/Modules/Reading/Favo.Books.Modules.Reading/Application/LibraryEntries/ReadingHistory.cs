using Favo.Books.Modules.Reading.Domain.LibraryEntries;
using Favo.Books.Modules.Reading.Persistence;
using Favo.Books.SharedKernel.Reading;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Reading.Application.LibraryEntries;

internal sealed class ReadingHistory(ReadingDbContext db) : IReadingHistory
{
    public async Task<IReadOnlyList<Guid>> GetWantToReadBookIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await db.LibraryEntries.AsNoTracking()
            .Where(e => e.UserId == userId && e.Status == LibraryEntryStatus.WantToRead)
            .Select(e => e.BookId).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetStartedBookIdsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await db.LibraryEntries.AsNoTracking()
            .Where(e => e.UserId == userId && (e.Status == LibraryEntryStatus.Read || e.Status == LibraryEntryStatus.Reading))
            .Select(e => e.BookId).ToArrayAsync(cancellationToken);
}
