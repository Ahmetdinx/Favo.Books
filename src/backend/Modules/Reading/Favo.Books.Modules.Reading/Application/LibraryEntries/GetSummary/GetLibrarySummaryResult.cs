namespace Favo.Books.Modules.Reading
    .Application.LibraryEntries.GetSummary;

internal sealed record GetLibrarySummaryResult(
    int TotalCount,
    int WantToReadCount,
    int ReadingCount,
    int ReadCount,
    int OnHoldCount,
    int DroppedCount);