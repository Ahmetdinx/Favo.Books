using Favo.Books.Modules.Catalog.Persistence;
using Favo.Books.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Catalog.Application.Books;

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
