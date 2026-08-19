using System.Text.Json.Serialization;

namespace KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary.Contracts;

internal sealed class OpenLibraryWorkEditionsResponse
{
    [JsonPropertyName("entries")]
    public OpenLibraryEditionResponse[] Entries
    {
        get;
        init;
    } = [];
}

internal sealed class OpenLibraryEditionResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("languages")]
    public OpenLibraryEditionLanguageResponse[] Languages
    {
        get;
        init;
    } = [];

    [JsonPropertyName("physical_format")]
    public string? PhysicalFormat { get; init; }

    [JsonPropertyName("isbn_10")]
    public string[] Isbn10Values { get; init; } = [];

    [JsonPropertyName("isbn_13")]
    public string[] Isbn13Values { get; init; } = [];

    [JsonPropertyName("number_of_pages")]
    public int? NumberOfPages { get; init; }

    [JsonPropertyName("publish_date")]
    public string? PublishDate { get; init; }

    [JsonPropertyName("covers")]
    public int[] CoverIds { get; init; } = [];

    [JsonPropertyName("publishers")]
    public string[] Publishers { get; init; } = [];
}

internal sealed class OpenLibraryEditionLanguageResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }
}