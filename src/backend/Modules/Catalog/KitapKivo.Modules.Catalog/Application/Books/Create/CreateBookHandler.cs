using KitapKivo.Modules.Catalog.Domain.Books;
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

        Book book = new(
            command.Title,
            command.OriginalLanguageCode,
            command.OriginalTitle,
            command.Description,
            command.FirstPublishedYear);

        for (int index = 0;
             index < command.AuthorIds.Count;
             index++)
        {
            BookAuthor bookAuthor = new(
                book.Id,
                command.AuthorIds[index],
                index);

            book.Authors.Add(bookAuthor);
        }

        _dbContext.Books.Add(book);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return CreateBookResult.Success(book.Id);
    }
}