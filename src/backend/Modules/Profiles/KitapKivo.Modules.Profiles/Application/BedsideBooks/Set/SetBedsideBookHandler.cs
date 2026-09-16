using KitapKivo.Modules.Profiles
    .Domain.BedsideBooks;
using KitapKivo.Modules.Profiles.Persistence;

using Microsoft.EntityFrameworkCore;
using KitapKivo.SharedKernel.Catalog;

namespace KitapKivo.Modules.Profiles
    .Application.BedsideBooks.Set;

internal sealed class SetBedsideBookHandler
{
    private readonly ProfilesDbContext
        _dbContext;
    private readonly IBookCatalog _bookCatalog;

    public SetBedsideBookHandler(
        ProfilesDbContext dbContext,
        IBookCatalog bookCatalog)
    {
        _dbContext = dbContext;
        _bookCatalog = bookCatalog;
    }

    public async Task<SetBedsideBookResult?>
        HandleAsync(
            SetBedsideBookCommand command,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!await _bookCatalog.ExistsAsync(command.BookId, cancellationToken))
        {
            return null;
        }

        BedsideBook? bedsideBook =
            await _dbContext.BedsideBooks
                .SingleOrDefaultAsync(
                    book =>
                        book.UserId ==
                        command.UserId,
                    cancellationToken);

        DateTimeOffset now =
            DateTimeOffset.UtcNow;

        if (bedsideBook is null)
        {
            bedsideBook =
                BedsideBook.Create(
                    command.UserId,
                    command.BookId,
                    command.PersonalNote,
                    now);

            _dbContext.BedsideBooks.Add(
                bedsideBook);
        }
        else
        {
            bedsideBook.Change(
                command.BookId,
                command.PersonalNote,
                now);
        }

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new SetBedsideBookResult(
            bedsideBook.Id,
            bedsideBook.BookId,
            bedsideBook.PersonalNote,
            bedsideBook.CreatedAtUtc,
            bedsideBook.UpdatedAtUtc);
    }
}
