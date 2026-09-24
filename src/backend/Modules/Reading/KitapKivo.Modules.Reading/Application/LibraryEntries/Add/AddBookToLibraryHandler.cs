using KitapKivo.Modules.Reading.Domain.LibraryEntries;
using KitapKivo.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;
using KitapKivo.SharedKernel.Catalog;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Add;

internal sealed class AddBookToLibraryHandler
{
    private readonly ReadingDbContext _dbContext;
    private readonly IBookCatalog _bookCatalog;

    public AddBookToLibraryHandler(
        ReadingDbContext dbContext,
        IBookCatalog bookCatalog)
    {
        _dbContext = dbContext;
        _bookCatalog = bookCatalog;
    }

    public async Task<AddBookToLibraryResult> HandleAsync(
        AddBookToLibraryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await _bookCatalog.ExistsAsync(command.BookId, cancellationToken))
        {
            return AddBookToLibraryResult.BookNotFound();
        }

        bool alreadyExists =
            await _dbContext.LibraryEntries
                .AsNoTracking()
                .AnyAsync(
                    entry =>
                        entry.UserId == command.UserId &&
                        entry.BookId == command.BookId,
                    cancellationToken);

        if (alreadyExists)
        {
            return AddBookToLibraryResult.AlreadyExists();
        }

        LibraryEntry libraryEntry = LibraryEntry.Create(
            command.UserId,
            command.BookId,
            command.Status,
            DateTimeOffset.UtcNow);

        _dbContext.LibraryEntries.Add(libraryEntry);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            return AddBookToLibraryResult.AlreadyExists();
        }

        return AddBookToLibraryResult.Success(
            libraryEntry.Id) with { UpdatedAtUtc = libraryEntry.UpdatedAtUtc };
    }
}
