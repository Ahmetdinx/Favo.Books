using KitapKivo.Modules.Profiles
    .Domain.InfluentialBooks;
using KitapKivo.Modules.Profiles.Persistence;

using Microsoft.EntityFrameworkCore;
using KitapKivo.SharedKernel.Catalog;

namespace KitapKivo.Modules.Profiles
    .Application.InfluentialBooks.Set;

internal sealed class SetInfluentialBookHandler
{
    private readonly ProfilesDbContext
        _dbContext;
    private readonly IBookCatalog _bookCatalog;

    public SetInfluentialBookHandler(
        ProfilesDbContext dbContext,
        IBookCatalog bookCatalog)
    {
        _dbContext = dbContext;
        _bookCatalog = bookCatalog;
    }

    public async Task<SetInfluentialBookResult?>
        HandleAsync(
            SetInfluentialBookCommand command,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!await _bookCatalog.ExistsAsync(command.BookId, cancellationToken))
        {
            return null;
        }

        bool bookAlreadyExists =
            await _dbContext.InfluentialBooks
                .AsNoTracking()
                .AnyAsync(
                    book =>
                        book.UserId ==
                            command.UserId &&
                        book.BookId ==
                            command.BookId &&
                        book.Position !=
                            command.Position,
                    cancellationToken);

        if (bookAlreadyExists)
        {
            return SetInfluentialBookResult
                .DuplicateBook();
        }

        InfluentialBook? influentialBook =
            await _dbContext.InfluentialBooks
                .SingleOrDefaultAsync(
                    book =>
                        book.UserId ==
                            command.UserId &&
                        book.Position ==
                            command.Position,
                    cancellationToken);

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        if (influentialBook is null)
        {
            influentialBook =
                InfluentialBook.Create(
                    command.UserId,
                    command.BookId,
                    command.Position,
                    command.PersonalReflection,
                    now);

            _dbContext.InfluentialBooks.Add(
                influentialBook);
        }
        else
        {
            influentialBook.Change(
                command.BookId,
                command.Position,
                command.PersonalReflection,
                now);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return SetInfluentialBookResult
            .Success(
                influentialBook.Id,
                influentialBook.BookId,
                influentialBook.Position,
                influentialBook
                    .PersonalReflection,
                influentialBook.CreatedAtUtc,
                influentialBook.UpdatedAtUtc);
    }
}
