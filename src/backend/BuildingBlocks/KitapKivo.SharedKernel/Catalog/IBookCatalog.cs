namespace KitapKivo.SharedKernel.Catalog;

/// <summary>
/// Read-only contract for validating local book references across modules.
/// Implemented by Catalog; consumers never access its persistence types.
/// </summary>
public interface IBookCatalog
{
    Task<bool> ExistsAsync(
        Guid bookId,
        CancellationToken cancellationToken = default);
}
