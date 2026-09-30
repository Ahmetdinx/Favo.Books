using Favo.Books.Modules.Reading
    .Domain.LibraryEntries;
using Favo.Books.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.GetSummary;

internal sealed class GetLibrarySummaryHandler
{
    private readonly ReadingDbContext _dbContext;

    public GetLibrarySummaryHandler(
        ReadingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLibrarySummaryResult>
        HandleAsync(
            GetLibrarySummaryQuery query,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var statusCounts =
            await _dbContext.LibraryEntries
                .AsNoTracking()
                .Where(entry =>
                    entry.UserId == query.UserId)
                .GroupBy(entry =>
                    entry.Status)
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .ToArrayAsync(cancellationToken);

        int GetCount(
            LibraryEntryStatus status)
        {
            return statusCounts
                .Where(item =>
                    item.Status == status)
                .Select(item =>
                    item.Count)
                .SingleOrDefault();
        }

        return new GetLibrarySummaryResult(
            statusCounts.Sum(item =>
                item.Count),
            GetCount(
                LibraryEntryStatus.WantToRead),
            GetCount(
                LibraryEntryStatus.Reading),
            GetCount(
                LibraryEntryStatus.Read),
            GetCount(
                LibraryEntryStatus.OnHold),
            GetCount(
                LibraryEntryStatus.Dropped));
    }
}