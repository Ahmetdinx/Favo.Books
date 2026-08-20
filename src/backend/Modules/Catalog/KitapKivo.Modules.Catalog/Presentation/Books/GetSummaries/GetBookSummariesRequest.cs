namespace KitapKivo.Modules.Catalog.Presentation
    .Books.GetSummaries;

public sealed record GetBookSummariesRequest(
    IReadOnlyList<Guid>? BookIds);