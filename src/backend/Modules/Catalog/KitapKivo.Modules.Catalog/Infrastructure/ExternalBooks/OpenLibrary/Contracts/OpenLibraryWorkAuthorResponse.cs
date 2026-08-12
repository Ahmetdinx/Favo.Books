using System.Text.Json.Serialization;

namespace KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary.Contracts;

internal sealed class OpenLibraryWorkAuthorResponse
{
    [JsonPropertyName("author")]
    public OpenLibraryAuthorKeyResponse? Author
    {
        get;
        init;
    }
}

internal sealed class OpenLibraryAuthorKeyResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}