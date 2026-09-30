using Favo.Books.Modules.Reading.Domain.LibraryEntries;
using Favo.Books.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.Get;

internal sealed class GetLibraryHandler
{
    private readonly ReadingDbContext _dbContext;

    public GetLibraryHandler(
        ReadingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLibraryResult> HandleAsync(
        GetLibraryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<LibraryEntry> libraryQuery =
            _dbContext.LibraryEntries
                .AsNoTracking()
                .Where(entry =>
                    entry.UserId == query.UserId);

        if (query.Status.HasValue)
        {
            libraryQuery = libraryQuery.Where(entry =>
                entry.Status == query.Status.Value);
        }

        int totalCount =
            await libraryQuery.CountAsync(
                cancellationToken);

        GetLibraryItemResult[] items =
            await libraryQuery
                .OrderByDescending(entry =>
                    entry.UpdatedAtUtc)
                .ThenByDescending(entry =>
                    entry.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(entry =>
                    new GetLibraryItemResult(
                        entry.Id,
                        entry.BookId,
                        entry.Status,
                        entry.CreatedAtUtc,
                        entry.UpdatedAtUtc))
                .ToArrayAsync(cancellationToken);

        return new GetLibraryResult(
            totalCount,
            query.Page,
            query.PageSize,
            items);
    }
}