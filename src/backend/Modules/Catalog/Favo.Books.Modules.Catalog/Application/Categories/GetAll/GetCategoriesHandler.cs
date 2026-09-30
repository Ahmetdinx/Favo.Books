using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Favo.Books.Modules.Catalog.Application.Books;

namespace Favo.Books.Modules.Catalog.Application.Categories.GetAll;

public sealed class GetCategoriesHandler
{
    private readonly CatalogDbContext _dbContext;

    public GetCategoriesHandler(
        CatalogDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetCategoriesResult> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var visibleBookIds = _dbContext.Books.EligibleNovels().Select(book => book.Id);

        GetCategoryItemResult[] items =
            await _dbContext.Categories
                .AsNoTracking()
                .Where(category => NovelGenreCatalog.Slugs.Contains(category.Slug))
                .OrderBy(category => category.Name)
                .Select(category =>
                    new GetCategoryItemResult(
                        category.Id,
                        category.Name,
                        category.Slug,
                        category.Books.Count(book => visibleBookIds.Contains(book.BookId))))
                .ToArrayAsync(cancellationToken);

        return new GetCategoriesResult(
            items.Length,
            items);
    }
}
