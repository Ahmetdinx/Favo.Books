namespace Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;

public interface IExternalBookDetailsClient
{
    Task<ExternalBookDetails?> GetByIdAsync(
        string externalId,
        CancellationToken cancellationToken = default);
}