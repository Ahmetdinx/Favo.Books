using KitapKivo.Modules.Community.Domain.Ratings;
using KitapKivo.Modules.Community.Persistence;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Community.Application.Ratings;

internal sealed class GetBookRatingsHandler(
    CommunityDbContext dbContext,
    IBookCatalog bookCatalog)
{
    public async Task<RatingResult?> HandleAsync(
        Guid bookId, Guid? userId, CancellationToken cancellationToken = default)
    {
        if (!await bookCatalog.ExistsAsync(bookId, cancellationToken)) return null;

        // SQL averages ignore legacy NULLs. Preserve precision for future sorting.
        var aggregate = await dbContext.BookRatings.AsNoTracking()
            .Where(rating => rating.BookId == bookId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(rating => rating.OverallScore != null),
                LegacyCount = group.Count(rating => rating.LegacyScore != null),
                Overall = group.Average(rating => rating.OverallScore),
                Theme = group.Average(rating => (decimal?)rating.Theme),
                Language = group.Average(rating => (decimal?)rating.Language),
                Characters = group.Average(rating => (decimal?)rating.Characters),
                Structure = group.Average(rating => (decimal?)rating.Structure),
                Pace = group.Average(rating => (decimal?)rating.Pace),
                Impact = group.Average(rating => (decimal?)rating.Impact),
                Experience = group.Average(rating => (decimal?)rating.Experience)
            })
            .SingleOrDefaultAsync(cancellationToken);

        BookRating? own = userId.HasValue
            ? await dbContext.BookRatings.AsNoTracking()
                .SingleOrDefaultAsync(rating => rating.BookId == bookId &&
                    rating.UserId == userId.Value, cancellationToken)
            : null;

        UserRatingResult? currentUserRating = own?.OverallScore is decimal overall
            ? new(new CriterionScores(own.Theme!.Value, own.Language!.Value,
                own.Characters!.Value, own.Structure!.Value, own.Pace!.Value,
                own.Impact!.Value, own.Experience!.Value), overall)
            : null;

        CriterionAverages? averages = aggregate is { Count: > 0 }
            ? new(aggregate.Theme!.Value, aggregate.Language!.Value,
                aggregate.Characters!.Value, aggregate.Structure!.Value,
                aggregate.Pace!.Value, aggregate.Impact!.Value, aggregate.Experience!.Value)
            : null;

        return new(bookId, aggregate?.Overall, aggregate?.Count ?? 0, averages,
            currentUserRating, aggregate?.LegacyCount ?? 0, own?.LegacyScore);
    }
}
