using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.EntityFrameworkCore;
namespace KitapKivo.Modules.Catalog.Application.Books;

internal sealed class NovelAchievementCatalog(CatalogDbContext db) : INovelAchievementCatalog
{
    public async Task<int> CountEligibleAsync(Guid[] bookIds, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var batch in bookIds.Distinct().Chunk(500))
            count += await db.Books.AsNoTracking().EligibleNovels()
                .CountAsync(b => batch.Contains(b.Id), cancellationToken);
        return count;
    }
}
