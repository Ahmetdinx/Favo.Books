using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application.Categories.GetAll;

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

        GetCategoryItemResult[] items =
            await _dbContext.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .Select(category =>
                    new GetCategoryItemResult(
                        category.Id,
                        category.Name,
                        category.Slug,
                        category.Books.Count))
                .ToArrayAsync(cancellationToken);

        return new GetCategoriesResult(
            items.Length,
            items);
    }
}