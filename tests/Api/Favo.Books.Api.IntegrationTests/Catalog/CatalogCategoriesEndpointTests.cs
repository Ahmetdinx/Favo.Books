using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class CatalogCategoriesEndpointTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private readonly FavoBooksWebApplicationFactory _factory;

    public CatalogCategoriesEndpointTests(
        FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCategories_ReturnsSeededCategories()
    {
        // Arrange
        using HttpClient client =
            _factory.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/api/catalog/categories");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kategori yanıtının gövdesi boş döndü.");

        JsonElement root =
            responseBody.RootElement;

        Assert.Equal(
            11,
            root.GetProperty("totalCount").GetInt32());

        JsonElement[] items = root
            .GetProperty("items")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(11, items.Length);

        Dictionary<string, string> expectedCategories =
            new()
            {
                ["Klasikler"] = "klasikler",
                ["Bilim Kurgu"] = "bilim-kurgu",
                ["Fantastik"] = "fantastik",
                ["Polisiye ve Gizem"] =
                    "polisiye-ve-gizem",
                ["Tarihî Roman"] = "tarihi-roman",
                ["Psikolojik Roman"] = "psikolojik-roman",
                ["Aşk ve Romantik"] = "ask-ve-romantik",
                ["Macera"] = "macera",
                ["Gerilim"] = "gerilim",
                ["Korku"] = "korku",
                ["Distopya"] = "distopya"
            };

        foreach (JsonElement item in items)
        {
            Guid categoryId =
                item.GetProperty("categoryId").GetGuid();

            string name =
                item.GetProperty("name").GetString()
                ?? throw new InvalidOperationException(
                    "Kategori adı boş döndü.");

            string slug =
                item.GetProperty("slug").GetString()
                ?? throw new InvalidOperationException(
                    "Kategori slug değeri boş döndü.");

            Assert.NotEqual(Guid.Empty, categoryId);

            Assert.True(
                expectedCategories.TryGetValue(
                    name,
                    out string? expectedSlug),
                $"Beklenmeyen kategori döndü: {name}");

            Assert.Equal(expectedSlug, slug);

            Assert.Equal(
                0,
                item.GetProperty("bookCount").GetInt32());
        }
    }
}
