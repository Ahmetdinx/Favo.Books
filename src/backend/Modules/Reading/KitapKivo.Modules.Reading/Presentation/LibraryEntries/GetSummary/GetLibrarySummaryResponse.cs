namespace KitapKivo.Modules.Reading
    .Presentation.LibraryEntries.GetSummary;

public sealed record GetLibrarySummaryResponse(
    int TotalCount,
    int WantToReadCount,
    int ReadingCount,
    int ReadCount,
    int OnHoldCount,
    int DroppedCount);