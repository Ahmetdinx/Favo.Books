using Favo.Books.Modules.Profiles
    .Domain.BedsideBooks;
using Favo.Books.Modules.Profiles.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Remove;

internal sealed class RemoveBedsideBookHandler
{
    private readonly ProfilesDbContext
        _dbContext;

    public RemoveBedsideBookHandler(
        ProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RemoveBedsideBookResult>
        HandleAsync(
            RemoveBedsideBookCommand command,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        BedsideBook? bedsideBook =
            await _dbContext.BedsideBooks
                .SingleOrDefaultAsync(
                    book =>
                        book.UserId ==
                        command.UserId,
                    cancellationToken);

        if (bedsideBook is null)
        {
            return RemoveBedsideBookResult
                .NotFound();
        }

        _dbContext.BedsideBooks.Remove(
            bedsideBook);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return RemoveBedsideBookResult
            .Success();
    }
}