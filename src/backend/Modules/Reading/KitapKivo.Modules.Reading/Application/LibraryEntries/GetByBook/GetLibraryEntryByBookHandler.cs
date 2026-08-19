using KitapKivo.Modules.Reading
    .Domain.LibraryEntries;
using KitapKivo.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.GetByBook;

internal sealed class GetLibraryEntryByBookHandler
{
    private readonly ReadingDbContext _dbContext;

    public GetLibraryEntryByBookHandler(
        ReadingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLibraryEntryByBookResult?>
        HandleAsync(
            GetLibraryEntryByBookQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _dbContext.LibraryEntries
            .AsNoTracking()
            .Where(entry =>
                entry.UserId == query.UserId &&
                entry.BookId == query.BookId)
            .Select(entry =>
                new GetLibraryEntryByBookResult(
                    entry.Id,
                    entry.BookId,
                    entry.Status,
                    entry.CreatedAtUtc,
                    entry.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }
}