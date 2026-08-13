namespace KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;

public interface IExternalBookEditionClient
{
    Task<ExternalBookEditionDetails?>
        GetBestForWorkAsync(
            string externalWorkId,
            CancellationToken cancellationToken = default);
}