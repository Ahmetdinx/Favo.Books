using Favo.Books.Modules.Profiles.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Get;

internal sealed class GetBedsideBookHandler
{
    private readonly ProfilesDbContext
        _dbContext;

    public GetBedsideBookHandler(
        ProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBedsideBookResult?>
        HandleAsync(
            GetBedsideBookQuery query,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        return await _dbContext.BedsideBooks
            .AsNoTracking()
            .Where(book =>
                book.UserId == query.UserId)
            .Select(book =>
                new GetBedsideBookResult(
                    book.Id,
                    book.BookId,
                    book.PersonalNote,
                    book.CreatedAtUtc,
                    book.UpdatedAtUtc))
            .SingleOrDefaultAsync(
                cancellationToken);
    }
}