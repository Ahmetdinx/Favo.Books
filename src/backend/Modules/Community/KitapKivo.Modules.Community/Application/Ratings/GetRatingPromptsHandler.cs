using KitapKivo.Modules.Community.Persistence;
using KitapKivo.SharedKernel.Catalog;
using KitapKivo.SharedKernel.Reading;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Community.Application.Ratings;

internal sealed class GetRatingPromptsHandler(
    CommunityDbContext dbContext, ICompletedBooks completedBooks, IBookCatalog catalog)
{
    public async Task<RatingPromptsResult> HandleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const int batchSize = 100;
        List<Guid> prompts = [];
        // Filter before limiting the response: rated recent books must not hide older unrated ones.
        for (var offset = 0; prompts.Count < 3; offset += batchSize)
        {
            var batch = await completedBooks.GetBookIdsAsync(userId, offset, batchSize, cancellationToken);
            if (batch.Count == 0) break;
            var ids = batch.ToArray();
            var rated = (await dbContext.BookRatings.AsNoTracking()
                .Where(rating => rating.UserId == userId && rating.OverallScore != null && ids.Contains(rating.BookId))
                .Select(rating => rating.BookId).ToArrayAsync(cancellationToken)).ToHashSet();
            foreach (var id in batch)
            {
                if (!rated.Contains(id) && await catalog.ExistsAsync(id, cancellationToken)) prompts.Add(id);
                if (prompts.Count == 3) break;
            }
            if (batch.Count < batchSize) break;
        }
        return new(prompts);
    }
}

internal sealed record RatingPromptsResult(IReadOnlyList<Guid> BookIds);
