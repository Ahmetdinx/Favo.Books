using Favo.Books.Modules.Reading.Domain.LibraryEntries;
using Favo.Books.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.Remove;

internal sealed class RemoveBookFromLibraryHandler
{
    private readonly ReadingDbContext _dbContext;

    public RemoveBookFromLibraryHandler(
        ReadingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RemoveBookFromLibraryResult> HandleAsync(
        RemoveBookFromLibraryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        LibraryEntry? libraryEntry =
            await _dbContext.LibraryEntries
                .SingleOrDefaultAsync(
                    entry =>
                        entry.Id == command.LibraryEntryId &&
                        entry.UserId == command.UserId,
                    cancellationToken);

        if (libraryEntry is null)
        {
            return RemoveBookFromLibraryResult.NotFound();
        }

        _dbContext.LibraryEntries.Remove(libraryEntry);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return RemoveBookFromLibraryResult.Success();
    }
}