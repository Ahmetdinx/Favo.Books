using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application.Books;

internal sealed class BookCatalog(CatalogDbContext dbContext) : IBookCatalog
{
    public Task<bool> ExistsAsync(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Books.AsNoTracking()
            .AnyAsync(book => book.Id == bookId, cancellationToken);
    }
}
