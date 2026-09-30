namespace Favo.Books.Modules.Catalog.Application.Abstractions.ExternalBooks;

public sealed record ExternalBookDetails(
    string Provider,
    string ExternalId,
    string Title,
    string? Description,
    IReadOnlyList<ExternalBookAuthor> Authors,
    int? FirstPublishedYear,
    string? CoverUrl,
    IReadOnlyList<string> Isbns,
    IReadOnlyList<string> LanguageCodes,
    IReadOnlyList<string> Subjects);