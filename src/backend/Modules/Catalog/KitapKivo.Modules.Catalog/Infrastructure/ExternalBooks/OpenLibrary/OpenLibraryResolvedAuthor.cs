namespace KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;

internal sealed record OpenLibraryResolvedAuthor(
    string ExternalId,
    string Name,
    IReadOnlyList<string> AlternativeNames);