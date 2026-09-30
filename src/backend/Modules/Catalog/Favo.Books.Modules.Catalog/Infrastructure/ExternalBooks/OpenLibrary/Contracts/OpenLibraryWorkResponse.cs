using System.Text.Json;
using System.Text.Json.Serialization;

namespace Favo.Books.Modules.Catalog.Infrastructure.ExternalBooks
    .OpenLibrary.Contracts;

internal sealed class OpenLibraryWorkResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public JsonElement? Description { get; init; }

    [JsonPropertyName("first_publish_date")]
    public string? FirstPublishDate { get; init; }

    [JsonPropertyName("covers")]
    public int[] CoverIds { get; init; } = [];

    [JsonPropertyName("authors")]
    public OpenLibraryWorkAuthorResponse[] Authors
    {
        get;
        init;
    } = [];

    [JsonPropertyName("subjects")]
    public string[] Subjects { get; init; } = [];
}