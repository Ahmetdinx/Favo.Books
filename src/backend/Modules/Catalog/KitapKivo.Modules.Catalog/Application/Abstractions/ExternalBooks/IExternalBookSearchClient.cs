namespace KitapKivo.Modules.Catalog.Application.Abstractions.ExternalBooks;

public interface IExternalBookSearchClient
{
    Task<ExternalBookSearchResult> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}