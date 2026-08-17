using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KitapKivo.Modules.Catalog.Infrastructure
    .Imports;

internal sealed class CatalogImportJobWorker :
    BackgroundService
{
    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly CatalogImportJobWorkerOptions
        _options;

    private readonly ILogger<CatalogImportJobWorker>
        _logger;

    public CatalogImportJobWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<CatalogImportJobWorkerOptions> options,
        ILogger<CatalogImportJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        bool interruptedJobsRequeued = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope =
                    _scopeFactory.CreateScope();

                CatalogImportJobProcessor processor =
                    scope.ServiceProvider
                        .GetRequiredService<
                            CatalogImportJobProcessor>();

                if (!interruptedJobsRequeued)
                {
                    await processor
                        .RequeueInterruptedJobsAsync(
                            stoppingToken);

                    interruptedJobsRequeued = true;
                }

                bool processed =
                    await processor.ProcessNextAsync(
                        stoppingToken);

                TimeSpan delay = processed
                    ? TimeSpan.FromSeconds(
                        _options
                            .DelayBetweenPagesSeconds)
                    : TimeSpan.FromSeconds(
                        _options
                            .PollIntervalSeconds);

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(
                        delay,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken
                    .IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Catalog import worker encountered " +
                    "an unexpected error.");

                await Task.Delay(
                    TimeSpan.FromSeconds(
                        _options.PollIntervalSeconds),
                    stoppingToken);
            }
        }
    }
}