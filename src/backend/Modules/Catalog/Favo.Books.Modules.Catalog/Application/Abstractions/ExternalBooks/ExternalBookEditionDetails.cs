namespace Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;

public sealed record ExternalBookEditionDetails(
    string Provider,
    string ExternalId,
    string? Title,
    string? LanguageCode,
    string? Format,
    string? Isbn10,
    string? Isbn13,
    int? PageCount,
    int? PublicationYear,
    string? CoverUrl,
    string? PublisherName);