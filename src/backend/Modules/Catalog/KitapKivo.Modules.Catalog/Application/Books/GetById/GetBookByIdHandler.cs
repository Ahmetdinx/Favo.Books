using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application.Books.GetById;

public sealed class GetBookByIdHandler
{
    private readonly CatalogDbContext _dbContext;

    public GetBookByIdHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBookByIdResult?> HandleAsync(
        GetBookByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var book = await _dbContext.Books
            .AsNoTracking()
            .Where(book => book.Id == query.BookId)
            .Select(book => new
            {
                book.Id,
                book.Title,
                book.OriginalLanguageCode,
                book.OriginalTitle,
                book.Description,
                book.FirstPublishedYear
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (book is null)
        {
            return null;
        }

        GetBookAuthorResult[] authors =
            await _dbContext.Set<BookAuthor>()
                .AsNoTracking()
                .Where(bookAuthor =>
                    bookAuthor.BookId == query.BookId)
                .Join(
                    _dbContext.Authors.AsNoTracking(),
                    bookAuthor => bookAuthor.AuthorId,
                    author => author.Id,
                    (bookAuthor, author) => new
                    {
                        AuthorId = author.Id,
                        author.Name,
                        bookAuthor.DisplayOrder
                    })
                .OrderBy(author =>
                    author.DisplayOrder)
                .Select(author =>
                    new GetBookAuthorResult(
                        author.AuthorId,
                        author.Name,
                        author.DisplayOrder))
                .ToArrayAsync(cancellationToken);

        var editionRows =
            await _dbContext.Editions
                .AsNoTracking()
                .Where(edition =>
                    edition.BookId == query.BookId)
                .OrderBy(edition =>
                    edition.CreatedAtUtc)
                .ThenBy(edition =>
                    edition.Id)
                .Select(edition => new
                {
                    EditionId = edition.Id,
                    edition.Title,
                    edition.LanguageCode,
                    edition.Format,
                    edition.Isbn10,
                    edition.Isbn13,
                    edition.PageCount,
                    edition.PublicationYear,
                    edition.CoverImageUrl,
                    edition.PublisherId,
                    PublisherName =
                        edition.Publisher == null
                            ? null
                            : edition.Publisher.Name
                })
                .ToArrayAsync(cancellationToken);

        GetBookEditionResult[] editions = editionRows
            .Select(edition =>
                new GetBookEditionResult(
                    edition.EditionId,
                    edition.Title,
                    edition.LanguageCode,
                    edition.Format.ToString(),
                    edition.Isbn10,
                    edition.Isbn13,
                    edition.PageCount,
                    edition.PublicationYear,
                    edition.CoverImageUrl,
                    edition.PublisherId.HasValue &&
                    !string.IsNullOrWhiteSpace(
                        edition.PublisherName)
                        ? new GetBookPublisherResult(
                            edition.PublisherId.Value,
                            edition.PublisherName)
                        : null))
            .ToArray();

        return new GetBookByIdResult(
            book.Id,
            book.Title,
            book.OriginalLanguageCode,
            book.OriginalTitle,
            book.Description,
            book.FirstPublishedYear,
            authors,
            editions);
    }
}