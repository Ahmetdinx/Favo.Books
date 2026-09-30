using Favo.Books.Modules.Profiles.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Profiles
    .Application.InfluentialBooks.Get;

internal sealed class
    GetInfluentialBooksHandler
{
    private readonly ProfilesDbContext
        _dbContext;

    public GetInfluentialBooksHandler(
        ProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        GetInfluentialBooksResult>
        HandleAsync(
            GetInfluentialBooksQuery query,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(
            query);

        GetInfluentialBookItemResult[] items =
            await _dbContext.InfluentialBooks
                .AsNoTracking()
                .Where(book =>
                    book.UserId ==
                        query.UserId)
                .OrderBy(book =>
                    book.Position)
                .Select(book =>
                    new
                        GetInfluentialBookItemResult(
                            book.Id,
                            book.BookId,
                            book.Position,
                            book.PersonalReflection,
                            book.CreatedAtUtc,
                            book.UpdatedAtUtc))
                .ToArrayAsync(
                    cancellationToken);

        return new GetInfluentialBooksResult(
            items);
    }
}