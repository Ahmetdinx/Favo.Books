using KitapKivo.Modules.Catalog.Application.Abstractions.ExternalBooks;

namespace KitapKivo.Modules.Catalog.Application.Books.Search;

public sealed class SearchBooksHandler
{
    private readonly IExternalBookSearchClient _searchClient;

    public SearchBooksHandler(
        IExternalBookSearchClient searchClient)
    {
        _searchClient = searchClient;
    }

    public async Task<ExternalBookSearchResult> HandleAsync(
        SearchBooksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _searchClient.SearchAsync(
            query.Query,
            query.Page,
            query.PageSize,
            cancellationToken);
    }
}