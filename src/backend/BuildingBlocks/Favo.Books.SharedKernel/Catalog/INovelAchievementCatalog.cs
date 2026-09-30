namespace Favo.Books.SharedKernel.Catalog;

public interface INovelAchievementCatalog
{
    Task<int> CountEligibleAsync(Guid[] bookIds, CancellationToken cancellationToken = default);
}
