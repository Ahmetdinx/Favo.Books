using Favo.Books.Modules.Community.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Community.Application.Ratings;

internal sealed class RemoveBookRatingHandler(CommunityDbContext dbContext)
{
    public async Task<bool> HandleAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        int deleted = await dbContext.BookRatings
            .Where(rating => rating.UserId == userId && rating.BookId == bookId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
