using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application.Books.Create;

public sealed class CreateBookHandler
{
    private readonly CatalogDbContext _dbContext;

    public CreateBookHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateBookResult> HandleAsync(
        CreateBookCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.AuthorIds);

        Guid[] requestedAuthorIds = command.AuthorIds
            .Distinct()
            .ToArray();

        Guid[] existingAuthorIds = await _dbContext.Authors
            .AsNoTracking()
            .Where(author =>
                requestedAuthorIds.Contains(author.Id))
            .Select(author => author.Id)
            .ToArrayAsync(cancellationToken);

        HashSet<Guid> existingAuthorIdSet =
            existingAuthorIds.ToHashSet();

        Guid[] missingAuthorIds = requestedAuthorIds
            .Where(authorId =>
                !existingAuthorIdSet.Contains(authorId))
            .ToArray();

        if (missingAuthorIds.Length > 0)
        {
            return CreateBookResult.AuthorsNotFound(
                missingAuthorIds);
        }

        // This legacy command carries neither reviewed work identity nor a covered edition.
        // Keep validation compatibility, but never persist unreviewed books through this path.
        return new CreateBookResult(null, [], "novel_review_required");
    }
}
