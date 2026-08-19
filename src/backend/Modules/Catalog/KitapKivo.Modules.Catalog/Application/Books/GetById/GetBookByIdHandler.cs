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
                .ThenBy(author =>
                    author.AuthorId)
                .Select(author =>
                    new GetBookAuthorResult(
                        author.AuthorId,
                        author.Name,
                        author.DisplayOrder))
                .ToArrayAsync(cancellationToken);

        GetBookCategoryResult[] categories =
            await _dbContext.BookCategories
                .AsNoTracking()
                .Where(bookCategory =>
                    bookCategory.BookId == query.BookId)
                .Join(
                    _dbContext.Categories.AsNoTracking(),
                    bookCategory => bookCategory.CategoryId,
                    category => category.Id,
                    (_, category) => new
                    {
                        CategoryId = category.Id,
                        category.Name,
                        category.Slug
                    })
                .OrderBy(category =>
                    category.Name)
                .ThenBy(category =>
                    category.CategoryId)
                .Select(category =>
                    new GetBookCategoryResult(
                        category.CategoryId,
                        category.Name,
                        category.Slug))
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

        GetBookEditionResult? primaryEdition =
            SelectPrimaryEdition(editions);

        return new GetBookByIdResult(
            book.Id,
            book.Title,
            book.OriginalLanguageCode,
            book.OriginalTitle,
            book.Description,
            book.FirstPublishedYear,
            primaryEdition?.EditionId,
            primaryEdition?.CoverImageUrl,
            authors,
            categories,
            editions);
    }

    private static GetBookEditionResult? SelectPrimaryEdition(
        IEnumerable<GetBookEditionResult> editions)
    {
        return editions
            .OrderByDescending(edition =>
                !string.IsNullOrWhiteSpace(
                    edition.CoverImageUrl))
            .ThenByDescending(edition =>
                IsTurkishLanguage(
                    edition.LanguageCode))
            .ThenByDescending(edition =>
                !string.IsNullOrWhiteSpace(
                    edition.Isbn13))
            .ThenByDescending(edition =>
                !string.IsNullOrWhiteSpace(
                    edition.Isbn10))
            .ThenByDescending(edition =>
                edition.Publisher is not null)
            .ThenByDescending(edition =>
                edition.PageCount.HasValue)
            .ThenByDescending(edition =>
                edition.PublicationYear.HasValue)
            .ThenByDescending(edition =>
                edition.PublicationYear)
            .ThenBy(edition =>
                edition.EditionId)
            .FirstOrDefault();
    }

    private static bool IsTurkishLanguage(
        string languageCode)
    {
        return string.Equals(
                   languageCode,
                   "tr",
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   languageCode,
                   "tur",
                   StringComparison.OrdinalIgnoreCase);
    }
}