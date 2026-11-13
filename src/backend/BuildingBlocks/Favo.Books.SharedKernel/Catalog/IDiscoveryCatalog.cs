namespace Favo.Books.SharedKernel.Catalog;

public interface IDiscoveryCatalog
{
    Task<IReadOnlyList<DiscoveryCategory>> GetGenresAsync(CancellationToken cancellationToken = default);
    /// <summary>All visible novels, most popular first (same order as the catalog's popular sort).</summary>
    Task<IReadOnlyList<DiscoveryBook>> GetBooksAsync(CancellationToken cancellationToken = default);
    /// <summary>Public novel genres (and the classics mark) of the given visible novels; unknown or hidden books are omitted.</summary>
    Task<IReadOnlyDictionary<Guid, DiscoveryCategory[]>> GetGenresForBooksAsync(Guid[] bookIds, CancellationToken cancellationToken = default);
}

public sealed record DiscoveryCategory(string Slug, string Name);
public sealed record DiscoveryBook(Guid BookId, string Title, string CoverImageUrl,
    Guid[] AuthorIds, DiscoveryCategory[] Categories);
