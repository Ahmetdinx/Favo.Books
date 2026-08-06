namespace KitapKivo.Modules.Catalog.Presentation.Books.Search;

public sealed record SearchBooksResponse(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<SearchBookItemResponse> Items);

public sealed record SearchBookItemResponse(
    string Provider,
    string ExternalId,
    string Title,
    IReadOnlyList<SearchBookAuthorResponse> Authors,
    int? FirstPublishedYear,
    string? CoverUrl,
    IReadOnlyList<string> Isbns,
    IReadOnlyList<string> LanguageCodes);

public sealed record SearchBookAuthorResponse(
    string? ExternalId,
    string Name);