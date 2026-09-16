using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace KitapKivo.Api.IntegrationTests;

internal static class CatalogTestData
{
    public static async Task<Guid> CreateBookAsync(HttpClient client)
    {
        using HttpResponseMessage authorResponse = await client.PostAsJsonAsync(
            "/api/catalog/authors", new { Name = "Test Author" });
        Assert.Equal(HttpStatusCode.Created, authorResponse.StatusCode);
        using JsonDocument author = (await authorResponse.Content
            .ReadFromJsonAsync<JsonDocument>())!;

        using HttpResponseMessage bookResponse = await client.PostAsJsonAsync(
            "/api/catalog/books", new
            {
                Title = "Test Book",
                OriginalLanguageCode = "tr",
                AuthorIds = new[] { author.RootElement.GetProperty("authorId").GetGuid() }
            });
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);
        using JsonDocument book = (await bookResponse.Content
            .ReadFromJsonAsync<JsonDocument>())!;
        return book.RootElement.GetProperty("bookId").GetGuid();
    }
}
