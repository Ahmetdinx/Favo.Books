using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;

namespace KitapKivo.Modules.Catalog.Application
    .Books.GetExternalDetails;

public sealed class GetExternalBookDetailsHandler
{
    private readonly IExternalBookDetailsClient
        _detailsClient;

    public GetExternalBookDetailsHandler(
        IExternalBookDetailsClient detailsClient)
    {
        _detailsClient = detailsClient;
    }

    public async Task<ExternalBookDetails?> HandleAsync(
        GetExternalBookDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await _detailsClient.GetByIdAsync(
            query.ExternalId,
            cancellationToken);
    }
}