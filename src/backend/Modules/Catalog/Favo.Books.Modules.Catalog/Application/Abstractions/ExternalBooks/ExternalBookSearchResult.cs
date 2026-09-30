namespace Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;

public sealed record ExternalBookSearchResult(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<ExternalBookSearchItem> Items);

public sealed record ExternalBookSearchItem(
    string Provider,
    string ExternalId,
    string Title,
    IReadOnlyList<ExternalBookAuthor> Authors,
    int? FirstPublishedYear,
    string? CoverUrl,
    IReadOnlyList<string> Isbns,
    IReadOnlyList<string> LanguageCodes);

public sealed record ExternalBookAuthor(
    string? ExternalId,
    string Name);