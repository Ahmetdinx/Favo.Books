using KitapKivo.Modules.Catalog.Domain.Books;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application
    .Books.GetSummaries;

public sealed class GetBookSummariesHandler
{
    private readonly CatalogDbContext _dbContext;

    public GetBookSummariesHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBookSummariesResult>
        HandleAsync(
            GetBookSummariesQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        Guid[] distinctBookIds = query.BookIds
            .Distinct()
            .ToArray();

        if (distinctBookIds.Length == 0)
        {
            return new GetBookSummariesResult([]);
        }

        GetBookSummaryItemResult[] items =
            await _dbContext.Books
                .AsNoTracking()
                .Where(book =>
                    distinctBookIds.Contains(book.Id))
                .OrderBy(book => book.Id)
                .Select(book =>
                    new GetBookSummaryItemResult(
                        book.Id,
                        book.Editions
                            .Where(edition =>
                                edition.LanguageCode == "tur" ||
                                edition.LanguageCode == "tr")
                            .OrderByDescending(edition =>
                                edition.PublicationYear)
                            .Select(edition =>
                                edition.Title)
                            .FirstOrDefault()
                        ??
                        book.Editions
                            .Where(edition =>
                                edition.LanguageCode == "eng" ||
                                edition.LanguageCode == "en")
                            .OrderByDescending(edition =>
                                edition.PublicationYear)
                            .Select(edition =>
                                edition.Title)
                            .FirstOrDefault()
                        ??
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
                                new GetBookSummaryAuthorResult(
                                    bookAuthor.AuthorId,
                                    bookAuthor.Author.Name))
                            .ToArray(),
                        book.Categories
                            .OrderBy(bookCategory =>
                                bookCategory.Category.Name)
                            .Select(bookCategory =>
                                new GetBookSummaryCategoryResult(
                                    bookCategory.CategoryId,
                                    bookCategory.Category.Name,
                                    bookCategory.Category.Slug))
                            .ToArray()))
                .ToArrayAsync(cancellationToken);

        return new GetBookSummariesResult(items);
    }
}
