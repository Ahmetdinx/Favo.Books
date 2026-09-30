namespace Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;

public interface IExternalBookEditionClient
{
    Task<ExternalBookEditionDetails?>
        GetBestForWorkAsync(
            string externalWorkId,
            CancellationToken cancellationToken = default);

    async Task<
        IReadOnlyList<ExternalBookEditionDetails>>
        GetBestEditionsForWorkAsync(
            string externalWorkId,
            CancellationToken cancellationToken = default)
    {
        ExternalBookEditionDetails? bestEdition =
            await GetBestForWorkAsync(
                externalWorkId,
                cancellationToken);

        return bestEdition is null
            ? []
            : [bestEdition];
    }
}