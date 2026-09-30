namespace Favo.Books.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary;

internal sealed class OpenLibraryOptions
{
    public const string SectionName = "Catalog:OpenLibrary";

    public string BaseUrl { get; init; } = "https://openlibrary.org";

    public int TimeoutSeconds { get; init; } = 30;

    public string UserAgent { get; init; } = "FavoBooks/1.0";
    public int RequestsPerSecond { get; init; } = 1;
}