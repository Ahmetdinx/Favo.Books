namespace Favo.Books.Modules.Catalog.Application
    .Books.GetSummaries;

public sealed record GetBookSummariesQuery(
    IReadOnlyList<Guid> BookIds);