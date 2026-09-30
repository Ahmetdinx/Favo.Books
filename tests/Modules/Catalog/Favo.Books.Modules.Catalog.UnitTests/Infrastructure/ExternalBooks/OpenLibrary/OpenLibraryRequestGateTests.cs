using System.Diagnostics;

using Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

using Microsoft.Extensions.Options;

namespace Favo.Books.Modules.Catalog.UnitTests
    .Infrastructure.ExternalBooks.OpenLibrary;

public sealed class OpenLibraryRequestGateTests
{
    [Fact]
    public async Task
        WaitAsync_WhenCalledTwice_EnforcesMinimumInterval()
    {
        // Arrange
        using OpenLibraryRequestGate requestGate =
            CreateRequestGate(
                requestsPerSecond: 3);

        await requestGate.WaitAsync();

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        // Act
        await requestGate.WaitAsync();

        stopwatch.Stop();

        // Assert
        Assert.True(
            stopwatch.Elapsed >=
            TimeSpan.FromMilliseconds(250),
            $"Elapsed time was " +
            $"{stopwatch.Elapsed.TotalMilliseconds:F0} ms.");
    }

    [Fact]
    public async Task
        WaitAsync_WhenWaitingIsCancelled_ThrowsCancellation()
    {
        // Arrange
        using OpenLibraryRequestGate requestGate =
            CreateRequestGate(
                requestsPerSecond: 1);

        await requestGate.WaitAsync();

        using CancellationTokenSource
            cancellationTokenSource = new(
                TimeSpan.FromMilliseconds(50));

        // Act
        Task action = requestGate.WaitAsync(
            cancellationTokenSource.Token);

        // Assert
        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
                async () => await action);
    }

    private static OpenLibraryRequestGate
        CreateRequestGate(
            int requestsPerSecond)
    {
        OpenLibraryOptions options = new()
        {
            RequestsPerSecond =
                requestsPerSecond
        };

        return new OpenLibraryRequestGate(
            Options.Create(options));
    }
}