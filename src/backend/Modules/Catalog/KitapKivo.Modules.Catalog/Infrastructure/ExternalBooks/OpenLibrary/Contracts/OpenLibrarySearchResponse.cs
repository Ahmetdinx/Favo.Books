using System.Text.Json.Serialization;

namespace KitapKivo.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary.Contracts;

internal sealed class OpenLibrarySearchResponse
{
    [JsonPropertyName("numFound")]
    public int NumFound { get; init; }

    [JsonPropertyName("start")]
    public int Start { get; init; }

    [JsonPropertyName("docs")]
    public OpenLibrarySearchDocument[] Documents { get; init; } = [];
}

internal sealed class OpenLibrarySearchDocument
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("author_name")]
    public string[] AuthorNames { get; init; } = [];

    [JsonPropertyName("author_key")]
    public string[] AuthorKeys { get; init; } = [];

    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; init; }

    [JsonPropertyName("cover_i")]
    public int? CoverId { get; init; }

    [JsonPropertyName("isbn")]
    public string[] Isbns { get; init; } = [];

    [JsonPropertyName("language")]
    public string[] LanguageCodes { get; init; } = [];
}