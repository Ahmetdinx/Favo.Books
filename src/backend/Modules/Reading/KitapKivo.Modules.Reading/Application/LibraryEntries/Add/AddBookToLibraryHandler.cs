using KitapKivo.Modules.Reading.Domain.LibraryEntries;
using KitapKivo.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Reading
    .Application.LibraryEntries.Add;

internal sealed class AddBookToLibraryHandler
{
    private readonly ReadingDbContext _dbContext;

    public AddBookToLibraryHandler(
        ReadingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AddBookToLibraryResult> HandleAsync(
        AddBookToLibraryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

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

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return AddBookToLibraryResult.Success(
            libraryEntry.Id);
    }
}