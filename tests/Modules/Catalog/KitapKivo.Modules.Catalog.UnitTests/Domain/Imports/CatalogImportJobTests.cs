using KitapKivo.Modules.Catalog.Domain.Imports;

namespace KitapKivo.Modules.Catalog.UnitTests
    .Domain.Imports;

public sealed class CatalogImportJobTests
{
    [Fact]
    public void Constructor_ShouldCreatePendingJob()
    {
        // Act
        var job = new CatalogImportJob(
            " OpenLibrary ",
            " fantasy ",
            1,
            20,
            50);

        // Assert
        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal("OpenLibrary", job.Provider);
        Assert.Equal("fantasy", job.Query);
        Assert.Equal(1, job.StartPage);
        Assert.Equal(1, job.NextPage);
        Assert.Equal(20, job.PageSize);
        Assert.Equal(50, job.PageCount);
        Assert.Equal(
            CatalogImportJobStatus.Pending,
            job.Status);
        Assert.Equal(0, job.CompletedPageCount);
        Assert.Equal(0, job.ProcessedCount);
    }

    [Fact]
    public void RecordCompletedPage_ShouldAccumulateResultsAndCompleteJob()
    {
        // Arrange
        var job = new CatalogImportJob(
            "OpenLibrary",
            "fantasy",
            1,
            10,
            2);

        DateTimeOffset startedAtUtc =
            DateTimeOffset.UtcNow;

        job.Start(startedAtUtc);

        // Act
        job.RecordCompletedPage(
            totalAvailableCount: 100,
            processedCount: 10,
            createdCount: 7,
            existingCount: 2,
            notFoundCount: 0,
            failedCount: 1,
            completedAtUtc:
                startedAtUtc.AddSeconds(1));

        // Assert
        Assert.Equal(
            CatalogImportJobStatus.Pending,
            job.Status);

        Assert.Equal(2, job.NextPage);
        Assert.Equal(1, job.CompletedPageCount);

        job.Start(
            startedAtUtc.AddSeconds(2));

        job.RecordCompletedPage(
            totalAvailableCount: 100,
            processedCount: 10,
            createdCount: 5,
            existingCount: 5,
            notFoundCount: 0,
            failedCount: 0,
            completedAtUtc:
                startedAtUtc.AddSeconds(3));

        Assert.Equal(
            CatalogImportJobStatus
                .CompletedWithFailures,
            job.Status);

        Assert.Equal(3, job.NextPage);
        Assert.Equal(2, job.CompletedPageCount);
        Assert.Equal(20, job.ProcessedCount);
        Assert.Equal(12, job.CreatedCount);
        Assert.Equal(7, job.ExistingCount);
        Assert.Equal(1, job.FailedCount);
        Assert.NotNull(job.CompletedAtUtc);
    }

    [Fact]
    public void ScheduleRetry_ShouldMoveJobToWaitingState()
    {
        // Arrange
        var job = new CatalogImportJob(
            "OpenLibrary",
            "fantasy",
            1,
            10,
            1);

        DateTimeOffset startedAtUtc =
            DateTimeOffset.UtcNow;

        job.Start(startedAtUtc);

        DateTimeOffset nextAttemptAtUtc =
            startedAtUtc.AddMinutes(1);

        // Act
        job.ScheduleRetry(
            "OpenLibrary is unavailable.",
            nextAttemptAtUtc,
            startedAtUtc.AddSeconds(1));

        // Assert
        Assert.Equal(
            CatalogImportJobStatus.WaitingForRetry,
            job.Status);

        Assert.Equal(
            1,
            job.ConsecutiveFailureCount);

        Assert.Equal(
            "OpenLibrary is unavailable.",
            job.LastError);

        Assert.Equal(
            nextAttemptAtUtc,
            job.NextAttemptAtUtc);
    }
}