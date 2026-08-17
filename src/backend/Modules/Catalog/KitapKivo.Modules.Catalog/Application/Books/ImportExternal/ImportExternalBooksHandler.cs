using System.Text.Json;

using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;

namespace KitapKivo.Modules.Catalog.Application
    .Books.ImportExternal;

public sealed class ImportExternalBooksHandler
{
    private const string SupportedProvider =
        "OpenLibrary";

    private const int MaximumBatchSize = 20;

    private readonly IExternalBookSearchClient
        _searchClient;

    private readonly ResolveExternalBookHandler
        _resolveHandler;

    public ImportExternalBooksHandler(
        IExternalBookSearchClient searchClient,
        ResolveExternalBookHandler resolveHandler)
    {
        _searchClient = searchClient;
        _resolveHandler = resolveHandler;
    }

    public async Task<ImportExternalBooksResult>
        HandleAsync(
            ImportExternalBooksCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ValidateCommand(command);

        string provider = command.Provider.Trim();
        string query = command.Query.Trim();

        ExternalBookSearchResult searchResult =
            await _searchClient.SearchAsync(
                query,
                command.Page,
                command.PageSize,
                cancellationToken);

        int createdCount = 0;
        int existingCount = 0;
        int notFoundCount = 0;

        List<ImportExternalBookFailureResult> failures =
            [];

        foreach (ExternalBookSearchItem item
                 in searchResult.Items)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            try
            {
                ResolveExternalBookResult resolveResult =
                    await _resolveHandler.HandleAsync(
                        new ResolveExternalBookCommand(
                            provider,
                            item.ExternalId),
                        cancellationToken);

                if (!resolveResult.IsSuccess)
                {
                    notFoundCount++;
                    continue;
                }

                if (resolveResult.IsCreated)
                {
                    createdCount++;
                }
                else
                {
                    existingCount++;
                }
            }
            catch (OperationCanceledException)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                failures.Add(
                    CreateFailure(
                        item,
                        "External provider request timed out."));
            }
            catch (HttpRequestException)
            {
                failures.Add(
                    CreateFailure(
                        item,
                        "External provider request failed."));
            }
            catch (JsonException)
            {
                failures.Add(
                    CreateFailure(
                        item,
                        "External provider returned invalid data."));
            }
            catch (ArgumentException)
            {
                failures.Add(
                    CreateFailure(
                        item,
                        "External book identifier or data was invalid."));
            }
        }

        int processedCount =
            createdCount +
            existingCount +
            notFoundCount +
            failures.Count;

        return new ImportExternalBooksResult(
            searchResult.TotalCount,
            searchResult.Page,
            searchResult.PageSize,
            searchResult.Items.Count,
            processedCount,
            createdCount,
            existingCount,
            notFoundCount,
            failures.Count,
            failures);
    }

    private static void ValidateCommand(
        ImportExternalBooksCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            command.Provider);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            command.Query);

        if (!string.Equals(
                command.Provider.Trim(),
                SupportedProvider,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only the OpenLibrary provider is supported.",
                nameof(command));
        }

        if (command.Page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                "Page must be greater than zero.");
        }

        if (command.PageSize is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                $"Page size must be between 1 and " +
                $"{MaximumBatchSize}.");
        }
    }

    private static ImportExternalBookFailureResult
        CreateFailure(
            ExternalBookSearchItem item,
            string reason)
    {
        return new ImportExternalBookFailureResult(
            item.ExternalId,
            item.Title,
            reason);
    }
}