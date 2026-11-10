using Favo.Books.Modules.Catalog.Persistence;
using Favo.Books.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;
using Favo.Books.Modules.Catalog.Application.Categories;

namespace Favo.Books.Modules.Catalog.Application.Books;

internal sealed class DiscoveryCatalog(CatalogDbContext db) : IDiscoveryCatalog
{
    public async Task<IReadOnlyList<DiscoveryCategory>> GetGenresAsync(CancellationToken cancellationToken = default)
        => await db.Categories.AsNoTracking()
            .Where(c => NovelGenreCatalog.Slugs.Contains(c.Slug))
            .OrderBy(c => c.Name)
            .Select(c => new DiscoveryCategory(c.Slug, c.Name)).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DiscoveryCategory[]>> GetGenresForBooksAsync(Guid[] bookIds, CancellationToken cancellationToken = default)
    {
        if (bookIds.Length == 0) return new Dictionary<Guid, DiscoveryCategory[]>();
        var rows = await db.Books.AsNoTracking().EligibleNovels().Where(b => bookIds.Contains(b.Id))
            .Select(b => new
            {
                b.Id,
                Genres = b.Categories.Where(c => NovelGenreCatalog.Slugs.Contains(c.Category.Slug))
                    .Select(c => new DiscoveryCategory(c.Category.Slug, c.Category.Name)).ToArray()
            }).ToArrayAsync(cancellationToken);
        return rows.ToDictionary(r => r.Id, r => r.Genres);
    }

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
