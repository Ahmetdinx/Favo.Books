using Favo.Books.Modules.Profiles
    .Domain.InfluentialBooks;
using Favo.Books.Modules.Profiles.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Profiles
    .Application.InfluentialBooks.Remove;

internal sealed class
    RemoveInfluentialBookHandler
{
    private readonly ProfilesDbContext
        _dbContext;

    public RemoveInfluentialBookHandler(
        ProfilesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<
        RemoveInfluentialBookResult>
        HandleAsync(
            RemoveInfluentialBookCommand command,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        InfluentialBook? influentialBook =
            await _dbContext.InfluentialBooks
                .SingleOrDefaultAsync(
                    book =>
                        book.UserId ==
                            command.UserId &&
                        book.Position ==
                            command.Position,
                    cancellationToken);

        if (influentialBook is null)
        {
            return RemoveInfluentialBookResult
                .NotFound();
        }

        _dbContext.InfluentialBooks.Remove(
            influentialBook);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return RemoveInfluentialBookResult
            .Success();
    }
}