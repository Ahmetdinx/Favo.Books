using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class CatalogExternalBookDetailsEndpointTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private readonly FavoBooksWebApplicationFactory _factory;

    public CatalogExternalBookDetailsEndpointTests(
        FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetExternalBookDetails_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var expectedDetails = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL45804W",
            "Fantastic Mr Fox",
            "A clever fox protects his family.",
            [
                new ExternalBookAuthor(
                    "/authors/OL34184A",
                    "Roald Dahl")
            ],
            1970,
            "https://example.com/fantastic-mr-fox.jpg",
            ["9780141311287"],
            ["eng"],
            ["Children's stories"]);

        var detailsClient =
            new StubExternalBookDetailsClient(
                expectedDetails);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/api/catalog/books/external/details" +
                "?provider=openlibrary" +
                "&externalId=%2Fworks%2FOL45804W");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Dış kitap detay yanıtının gövdesi boş döndü.");

        JsonElement root = responseBody.RootElement;

        Assert.Equal(
            "OpenLibrary",
            root.GetProperty("provider").GetString());

        Assert.Equal(
            "/works/OL45804W",
            root.GetProperty("externalId").GetString());

        Assert.Equal(
            "Fantastic Mr Fox",
            root.GetProperty("title").GetString());

        Assert.Equal(
            1970,
            root.GetProperty("firstPublishedYear").GetInt32());

        JsonElement author = root
            .GetProperty("authors")
            .EnumerateArray()
            .Single();

        Assert.Equal(
            "/authors/OL34184A",
            author.GetProperty("externalId").GetString());

        Assert.Equal(
            "Roald Dahl",
            author.GetProperty("name").GetString());

        Assert.Equal(1, detailsClient.CallCount);

        Assert.Equal(
            "/works/OL45804W",
            detailsClient.ExternalId);
    }

    [Fact]
    public async Task GetExternalBookDetails_WithUnsupportedProvider_ReturnsBadRequest()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(null);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/api/catalog/books/external/details" +
                "?provider=GoogleBooks" +
                "&externalId=%2Fworks%2FOL45804W");

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Doğrulama yanıtının gövdesi boş döndü.");

        JsonElement providerErrors = responseBody
            .RootElement
            .GetProperty("errors")
            .GetProperty("provider");

        Assert.Equal(
            "Yalnızca OpenLibrary sağlayıcısı desteklenmektedir.",
            providerErrors[0].GetString());

        Assert.Equal(0, detailsClient.CallCount);
    }

    [Fact]
    public async Task GetExternalBookDetails_WhenBookDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(null);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        // Act
        using HttpResponseMessage response =
            await client.GetAsync(
                "/api/catalog/books/external/details" +
                "?provider=OpenLibrary" +
                "&externalId=%2Fworks%2FOL404W");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(1, detailsClient.CallCount);

        Assert.Equal(
            "/works/OL404W",
            detailsClient.ExternalId);
    }

    private WebApplicationFactory<Program> CreateApplication(
        IExternalBookDetailsClient detailsClient)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<
                    IExternalBookDetailsClient>();

                services.AddSingleton(detailsClient);
            });
        });
    }

    private sealed class StubExternalBookDetailsClient(
        ExternalBookDetails? result)
        : IExternalBookDetailsClient
    {
        public int CallCount { get; private set; }

        public string? ExternalId { get; private set; }

        public Task<ExternalBookDetails?> GetByIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ExternalId = externalId;

            return Task.FromResult(result);
        }
    }
}