using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application.Books.Browse;

public sealed class BrowseBooksHandler
{
    private readonly CatalogDbContext _dbContext;

    public BrowseBooksHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BrowseBooksResult> HandleAsync(
        BrowseBooksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        string? normalizedQuery =
            string.IsNullOrWhiteSpace(query.Query)
                ? null
                : query.Query.Trim();

        string? normalizedCategorySlug =
            string.IsNullOrWhiteSpace(query.CategorySlug)
                ? null
                : query.CategorySlug
                    .Trim()
                    .ToLowerInvariant();

        IQueryable<Book> books = _dbContext.Books
            .AsNoTracking();

        if (normalizedCategorySlug is not null)
        {
            books = books.Where(book =>
                book.Categories.Any(bookCategory =>
                    bookCategory.Category.Slug ==
                    normalizedCategorySlug));
        }

        if (normalizedQuery is not null)
        {
            string searchPattern =
                $"%{normalizedQuery}%";

            books = query.SearchField switch
            {
                CatalogBookSearchField.Title =>
                    books.Where(book =>
                        EF.Functions.ILike(
                            book.Title,
                            searchPattern) ||
                        (
                            book.OriginalTitle != null &&
                            EF.Functions.ILike(
                                book.OriginalTitle,
                                searchPattern)
                        ) ||
                        book.Editions.Any(edition =>
                            EF.Functions.ILike(
                                edition.Title,
                                searchPattern))),

                CatalogBookSearchField.Author =>
                    books.Where(book =>
                        book.Authors.Any(bookAuthor =>
                            EF.Functions.ILike(
                                bookAuthor.Author.Name,
                                searchPattern))),

                _ =>
                    books.Where(book =>
                        EF.Functions.ILike(
                            book.Title,
                            searchPattern) ||
                        (
                            book.OriginalTitle != null &&
                            EF.Functions.ILike(
                                book.OriginalTitle,
                                searchPattern)
                        ) ||
                        book.Editions.Any(edition =>
                            EF.Functions.ILike(
                                edition.Title,
                                searchPattern)) ||
                        book.Authors.Any(bookAuthor =>
                            EF.Functions.ILike(
                                bookAuthor.Author.Name,
                                searchPattern)))
            };
        }

        int totalCount =
            await books.CountAsync(cancellationToken);

        IQueryable<Book> orderedBooks =
            query.Sort switch
            {
                CatalogBookSort.Title =>
                    books
                        .OrderBy(book => book.Title)
                        .ThenBy(book => book.Id),

                CatalogBookSort.FirstPublishedNewest =>
                    books
                        .OrderByDescending(book =>
                            book.FirstPublishedYear.HasValue)
                        .ThenByDescending(book =>
                            book.FirstPublishedYear)
                        .ThenBy(book => book.Title),

                _ =>
                    books
                        .OrderByDescending(book =>
                            book.CreatedAtUtc)
                        .ThenBy(book => book.Title)
            };

        BrowseBookItemResult[] items =
            await orderedBooks
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(book =>
                    new BrowseBookItemResult(
                        book.Id,
                  book.Editions
    .Where(edition =>
        edition.LanguageCode == "tur" ||
        edition.LanguageCode == "tr")
    .OrderByDescending(edition =>
        edition.PublicationYear)
    .Select(edition =>
        edition.Title)
    .FirstOrDefault() ??
book.Editions
    .Where(edition =>
        edition.LanguageCode == "eng" ||
        edition.LanguageCode == "en")
    .OrderByDescending(edition =>
        edition.PublicationYear)
    .Select(edition =>
        edition.Title)
    .FirstOrDefault() ??
book.Title,
                        book.FirstPublishedYear,
book.Editions
    .Where(edition =>
        edition.CoverImageUrl != null &&
        (edition.LanguageCode == "tur" ||
         edition.LanguageCode == "tr"))
    .OrderByDescending(edition =>
        edition.LanguageCode == "tur" ||
        edition.LanguageCode == "tr")
    .ThenByDescending(edition =>
        edition.PublicationYear)
    .ThenBy(edition =>
        edition.Id)
    .Select(edition =>
        edition.CoverImageUrl)
    .FirstOrDefault(),
                        book.Authors
                            .OrderBy(bookAuthor =>
                                bookAuthor.DisplayOrder)
                            .Select(bookAuthor =>
                                new BrowseBookAuthorResult(
                                    bookAuthor.AuthorId,
                                    bookAuthor.Author.Name))
                            .ToArray(),
                        book.Categories
                            .OrderBy(bookCategory =>
                                bookCategory.Category.Name)
                            .Select(bookCategory =>
                                new BrowseBookCategoryResult(
                                    bookCategory.CategoryId,
                                    bookCategory.Category.Name,
                                    bookCategory.Category.Slug))
                            .ToArray()))
                .ToArrayAsync(cancellationToken);

        return new BrowseBooksResult(
            totalCount,
            query.Page,
            query.PageSize,
            items);
    }
}
