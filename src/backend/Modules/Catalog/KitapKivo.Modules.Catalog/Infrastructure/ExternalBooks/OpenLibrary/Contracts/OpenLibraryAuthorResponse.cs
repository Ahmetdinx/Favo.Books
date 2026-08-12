using System.Text.Json.Serialization;

namespace KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary.Contracts;

internal sealed class OpenLibraryAuthorResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("alternate_names")]
    public string[] AlternativeNames
    {
        get;
        init;
    } = [];
}