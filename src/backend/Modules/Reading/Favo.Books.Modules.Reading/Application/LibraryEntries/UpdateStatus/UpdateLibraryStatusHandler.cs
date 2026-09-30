using Favo.Books.Modules.Reading.Domain.LibraryEntries;
using Favo.Books.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.UpdateStatus;

internal sealed class UpdateLibraryStatusHandler
{
    private readonly ReadingDbContext _dbContext;

    public UpdateLibraryStatusHandler(
        ReadingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UpdateLibraryStatusResult> HandleAsync(
        UpdateLibraryStatusCommand command,
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
            return UpdateLibraryStatusResult.NotFound();
        }

        libraryEntry.ChangeStatus(
            command.Status,
            DateTimeOffset.UtcNow);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return UpdateLibraryStatusResult.Success(
            libraryEntry.Id,
            libraryEntry.Status,
            libraryEntry.UpdatedAtUtc);
    }
}