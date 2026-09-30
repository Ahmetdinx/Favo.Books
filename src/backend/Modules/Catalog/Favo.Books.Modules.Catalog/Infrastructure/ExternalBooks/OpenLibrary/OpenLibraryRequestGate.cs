using System.Diagnostics;

using Microsoft.Extensions.Options;

namespace Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

internal sealed class OpenLibraryRequestGate :
    IDisposable
{
    private readonly SemaphoreSlim _mutex =
        new(1, 1);

    private readonly long _minimumIntervalTicks;

    private long _nextRequestTimestamp;

    public OpenLibraryRequestGate(
        IOptions<OpenLibraryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        int requestsPerSecond =
            options.Value.RequestsPerSecond;

        if (requestsPerSecond is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Open Library requests per second must " +
                "be between 1 and 3.");
        }

        _minimumIntervalTicks =
            (long)Math.Ceiling(
                (double)Stopwatch.Frequency /
                requestsPerSecond);
    }

    public async Task WaitAsync(
        CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);

        try
        {
            long currentTimestamp =
                Stopwatch.GetTimestamp();

            long remainingTicks =
                _nextRequestTimestamp -
                currentTimestamp;

            if (remainingTicks > 0)
            {
                TimeSpan delay =
                    TimeSpan.FromSeconds(
                        (double)remainingTicks /
                        Stopwatch.Frequency);

                await Task.Delay(
                    delay,
                    cancellationToken);
            }

            _nextRequestTimestamp =
                Stopwatch.GetTimestamp() +
                _minimumIntervalTicks;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public void Dispose()
    {
        _mutex.Dispose();
    }
}