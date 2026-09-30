namespace Favo.Books.SharedKernel.Catalog;

public interface IDiscoveryCatalog
{
    Task<IReadOnlyList<DiscoveryCategory>> GetGenresAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiscoveryBook>> GetBooksAsync(CancellationToken cancellationToken = default);
}

public sealed record DiscoveryCategory(string Slug, string Name);
public sealed record DiscoveryBook(Guid BookId, string Title, string CoverImageUrl,
    Guid[] AuthorIds, DiscoveryCategory[] Categories);
