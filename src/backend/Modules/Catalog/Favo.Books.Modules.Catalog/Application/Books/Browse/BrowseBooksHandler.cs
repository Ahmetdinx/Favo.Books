using Favo.Books.Modules.Catalog.Domain.Books;
using Favo.Books.Modules.Catalog.Domain.Sources;
using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Application.Books.Browse;

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
        CancellationToken cancellationToken = default, Guid[]? demotedBookIds = null)
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
            .AsNoTracking().EligibleNovels();

        if (normalizedCategorySlug is not null)
        {
            books = books.Where(book =>
                book.Categories.Any(bookCategory =>
                    bookCategory.Category.Slug ==
                    normalizedCategorySlug));
        }

        const string from = CatalogSearchText.TurkishLetters;
        const string to = CatalogSearchText.PlainLetters;

        if (normalizedQuery is not null)
        {
            foreach (string term in CatalogSearchText.Terms(normalizedQuery))
            {
                string pattern = CatalogSearchText.Contains(term);
                string isbn = CatalogSearchText.IsbnDigits(term);

                books = query.SearchField switch
                {
                    CatalogBookSearchField.Title =>
                        books.Where(book =>
                            EF.Functions.ILike(CatalogSearchText.Fold(book.Title, from, to), pattern) ||
                            (book.OriginalTitle != null &&
                                EF.Functions.ILike(CatalogSearchText.Fold(book.OriginalTitle, from, to), pattern)) ||
                            book.Editions.Any(edition =>
                                EF.Functions.ILike(CatalogSearchText.Fold(edition.Title, from, to), pattern))),

                    CatalogBookSearchField.Author =>
                        books.Where(book =>
                            book.Authors.Any(bookAuthor =>
                                EF.Functions.ILike(CatalogSearchText.Fold(bookAuthor.Author.Name, from, to), pattern))),

                    _ =>
                        books.Where(book =>
                            EF.Functions.ILike(CatalogSearchText.Fold(book.Title, from, to), pattern) ||
                            (book.OriginalTitle != null &&
                                EF.Functions.ILike(CatalogSearchText.Fold(book.OriginalTitle, from, to), pattern)) ||
                            book.Editions.Any(edition =>
                                EF.Functions.ILike(CatalogSearchText.Fold(edition.Title, from, to), pattern) ||
                                edition.Isbn13 == isbn ||
                                edition.Isbn10 == isbn) ||
                            book.Authors.Any(bookAuthor =>
                                EF.Functions.ILike(CatalogSearchText.Fold(bookAuthor.Author.Name, from, to), pattern)))
                };
            }
        }

        int totalCount =
            await books.CountAsync(cancellationToken);

        var demoted = demotedBookIds ?? [];
        string? titlePrefix = normalizedQuery is null
            ? null
            : CatalogSearchText.StartsWith(CatalogSearchText.Normalize(normalizedQuery));
        var prioritized = books
            .OrderBy(book => demoted.Contains(book.Id))
            .ThenByDescending(book =>
                titlePrefix != null &&
                EF.Functions.ILike(CatalogSearchText.Fold(book.Title, from, to), titlePrefix));
        IQueryable<Book> orderedBooks =
            query.Sort switch
            {
                CatalogBookSort.Title =>
                    prioritized
                        .ThenBy(book => book.Title)
                        .ThenBy(book => book.Id),

                CatalogBookSort.Popular =>
                    prioritized
                        .ThenByDescending(book => book.ExternalReferences.Any(reference =>
                            reference.Source == CatalogSource.Manual))
                        .ThenBy(book => book.CreatedAtUtc)
                        .ThenBy(book => book.Id),

                CatalogBookSort.FirstPublishedNewest =>
                    prioritized
                        .ThenByDescending(book =>
                            book.FirstPublishedYear.HasValue)
                        .ThenByDescending(book =>
                            book.FirstPublishedYear)
                        .ThenBy(book => book.Title)
                        .ThenBy(book => book.Id),

                _ =>
                    prioritized
                        .ThenByDescending(book =>
                            book.CreatedAtUtc)
                        .ThenBy(book => book.Title)
                        .ThenBy(book => book.Id)
            };

        BrowseBookItemResult[] items =
            await orderedBooks
                .AsSplitQuery()
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
