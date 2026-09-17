using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Catalog.Application.Books;

internal sealed class DiscoveryCatalog(CatalogDbContext db) : IDiscoveryCatalog
{
    public async Task<IReadOnlyList<DiscoveryBook>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        return await db.Books.AsNoTracking()
            .EligibleNovels()
            .OrderBy(b => b.Id)
            .Select(b => new DiscoveryBook(b.Id, b.Title,
                b.Editions.Where(e => (e.LanguageCode == "tur" || e.LanguageCode == "tr")
                    && e.CoverImageUrl != null && e.CoverImageUrl != "")
                    .OrderByDescending(e => e.PublicationYear).ThenBy(e => e.Id).Select(e => e.CoverImageUrl!).First(),
                b.Authors.Select(a => a.AuthorId).ToArray(),
                b.Categories.Select(c => new DiscoveryCategory(c.Category.Slug, c.Category.Name)).ToArray()))
            .ToArrayAsync(cancellationToken);
    }
}
