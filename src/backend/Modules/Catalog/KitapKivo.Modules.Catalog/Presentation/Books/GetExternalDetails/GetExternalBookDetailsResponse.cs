namespace KitapKivo.Modules.Catalog.Presentation
    .Books.GetExternalDetails;

public sealed record GetExternalBookDetailsResponse(
    string Provider,
    string ExternalId,
    string Title,
    string? Description,
    IReadOnlyList<GetExternalBookAuthorResponse> Authors,
    int? FirstPublishedYear,
    string? CoverUrl,
    IReadOnlyList<string> Isbns,
    IReadOnlyList<string> LanguageCodes,
    IReadOnlyList<string> Subjects);

public sealed record GetExternalBookAuthorResponse(
    string? ExternalId,
    string Name);