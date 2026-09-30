using System.Text.Json.Serialization;

namespace Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary.Contracts;

internal sealed class OpenLibraryEditionSearchResponse
{
    [JsonPropertyName("numFound")]
    public int NumFound { get; init; }

    [JsonPropertyName("docs")]
    public OpenLibraryEditionSearchDocument[] Documents
    {
        get;
        init;
    } = [];
}

internal sealed class OpenLibraryEditionSearchDocument
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("editions")]
    public OpenLibraryEditionSearchGroup? Editions
    {
        get;
        init;
    }
}

internal sealed class OpenLibraryEditionSearchGroup
{
    [JsonPropertyName("numFound")]
    public int NumFound { get; init; }

    [JsonPropertyName("docs")]
    public OpenLibraryEditionSearchItem[] Documents
    {
        get;
        init;
    } = [];
}

internal sealed class OpenLibraryEditionSearchItem
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}