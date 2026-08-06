namespace KitapKivo.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary;

internal sealed class OpenLibraryOptions
{
    public const string SectionName = "Catalog:OpenLibrary";

    public string BaseUrl { get; init; } = "https://openlibrary.org";

    public int TimeoutSeconds { get; init; } = 15;

    public string UserAgent { get; init; } = "KitapKivo/1.0";
}