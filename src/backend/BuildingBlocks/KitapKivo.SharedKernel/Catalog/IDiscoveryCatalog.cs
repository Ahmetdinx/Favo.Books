namespace KitapKivo.SharedKernel.Catalog;

public interface IDiscoveryCatalog
{
    Task<IReadOnlyList<DiscoveryBook>> GetBooksAsync(CancellationToken cancellationToken = default);
}

public sealed record DiscoveryCategory(string Slug, string Name);
public sealed record DiscoveryBook(Guid BookId, string Title, string CoverImageUrl,
    Guid[] AuthorIds, DiscoveryCategory[] Categories);
