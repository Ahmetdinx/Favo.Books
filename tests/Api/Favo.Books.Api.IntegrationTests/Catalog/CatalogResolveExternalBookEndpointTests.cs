using Favo.Books.Modules.Catalog.Application.Books;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class CatalogResolveExternalBookEndpointTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private readonly FavoBooksWebApplicationFactory _factory;

    public CatalogResolveExternalBookEndpointTests(
        FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResolveExternalBook_WhenBookDoesNotExist_ReturnsCreated()
    {
        // Arrange
        var details = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL11001W",
            "Dune Messiah",
            "The second novel in the Dune series.",
            [
                new ExternalBookAuthor(
                    "/authors/OL21001A",
                    "Frank Herbert")
            ],
            1969,
            "https://example.com/dune-messiah.jpg",
            ["9780441172696"],
            ["eng"],
            ["Science Fiction"]);

        var detailsClient =
            new StubExternalBookDetailsClient(details);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        await AuthenticateAsync(client);

        var request = new
        {
            Provider = "OpenLibrary",
            ExternalId = "/works/OL11001W"
        };

        // Act
        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/catalog/books/external/resolve",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Dış kitap çözümleme yanıtının gövdesi boş döndü.");

        JsonElement root = responseBody.RootElement;

        Guid bookId =
            root.GetProperty("bookId").GetGuid();

        Assert.NotEqual(Guid.Empty, bookId);

        Assert.True(
            root.GetProperty("isCreated").GetBoolean());

        Assert.Equal(
            $"/api/catalog/books/{bookId}",
            response.Headers.Location?.OriginalString);

        Assert.Equal(1, detailsClient.CallCount);

        Assert.Equal(
            "/works/OL11001W",
            detailsClient.ExternalId);
    }

    [Fact]
    public async Task ResolveExternalBook_WhenBookAlreadyExists_ReturnsOk()
    {
        // Arrange
        var details = new ExternalBookDetails(
            "OpenLibrary",
            "/works/OL11002W",
            "Existing External Book",
            null,
            [new ExternalBookAuthor(null, "Test Author")],
            2001,
            null,
            [],
            ["eng"],
            []);

        var detailsClient =
            new StubExternalBookDetailsClient(details);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        await AuthenticateAsync(client);

        var request = new
        {
            Provider = "OpenLibrary",
            ExternalId = "/works/OL11002W"
        };

        using HttpResponseMessage firstResponse =
            await client.PostAsJsonAsync(
                "/api/catalog/books/external/resolve",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        using JsonDocument firstResponseBody =
            await firstResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "İlk çözümleme yanıtının gövdesi boş döndü.");

        Guid firstBookId =
            firstResponseBody.RootElement
                .GetProperty("bookId")
                .GetGuid();

        // Act
        using HttpResponseMessage secondResponse =
            await client.PostAsJsonAsync(
                "/api/catalog/books/external/resolve",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode);

        using JsonDocument secondResponseBody =
            await secondResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "İkinci çözümleme yanıtının gövdesi boş döndü.");

        JsonElement root =
            secondResponseBody.RootElement;

        Assert.Equal(
            firstBookId,
            root.GetProperty("bookId").GetGuid());

        Assert.False(
            root.GetProperty("isCreated").GetBoolean());

        Assert.Equal(1, detailsClient.CallCount);
    }

    [Fact]
    public async Task ResolveExternalBook_WhenBookDoesNotExistExternally_ReturnsNotFound()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(null);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        await AuthenticateAsync(client);

        var request = new
        {
            Provider = "OpenLibrary",
            ExternalId = "/works/OL11999W"
        };

        // Act
        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/catalog/books/external/resolve",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(1, detailsClient.CallCount);

        Assert.Equal(
            "/works/OL11999W",
            detailsClient.ExternalId);
    }

    [Fact]
    public async Task ResolveExternalBook_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(null);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        await AuthenticateAsync(client);

        var request = new
        {
            Provider = "GoogleBooks",
            ExternalId = "invalid-book-id"
        };

        // Act
        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/catalog/books/external/resolve",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Doğrulama yanıtının gövdesi boş döndü.");

        JsonElement errors =
            responseBody.RootElement.GetProperty("errors");

        Assert.Equal(
            "Yalnızca OpenLibrary sağlayıcısı desteklenmektedir.",
            errors.GetProperty("provider")[0].GetString());

        Assert.Equal(
            "Open Library kitap kimliği " +
            "'/works/OL...W' biçiminde olmalıdır.",
            errors.GetProperty("externalId")[0].GetString());

        Assert.Equal(0, detailsClient.CallCount);
    }

    [Fact]
    public async Task ResolveExternalBook_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        var detailsClient =
            new StubExternalBookDetailsClient(null);

        using WebApplicationFactory<Program> application =
            CreateApplication(detailsClient);

        using HttpClient client =
            application.CreateClient();

        var request = new
        {
            Provider = "OpenLibrary",
            ExternalId = "/works/OL11005W"
        };

        // Act
        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/catalog/books/external/resolve",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(0, detailsClient.CallCount);
    }

    private WebApplicationFactory<Program> CreateApplication(
        IExternalBookDetailsClient detailsClient)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IApprovedNovelCatalog>();
                services.AddSingleton<IApprovedNovelCatalog>(new FixtureNovelCatalog((IFixtureNovelDetails)detailsClient));

                services.RemoveAll<
                    IExternalBookDetailsClient>();

                services.AddSingleton(detailsClient);
            });
        });
    }

    private async Task AuthenticateAsync(
        HttpClient client)
    {
        string email =
            $"catalog-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        var credentials = new
        {
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
            Password = password
        };

        using HttpResponseMessage registerResponse =
            await client.PostAsJsonAsync(
                "/api/identity/register",
                credentials);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        await _factory.PromoteToAdminAsync(email);

        using HttpResponseMessage loginResponse =
            await client.PostAsJsonAsync(
                "/api/identity/login",
                credentials);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using JsonDocument loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Login yanıtının gövdesi boş döndü.");

        string accessToken =
            loginBody.RootElement
                .GetProperty("accessToken")
                .GetString()
            ?? throw new InvalidOperationException(
                "Login yanıtında access token bulunamadı.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
    }

    private sealed class StubExternalBookDetailsClient(
        ExternalBookDetails? result)
        : IExternalBookDetailsClient, IFixtureNovelDetails
    {
        public ExternalBookDetails? Fixture(string id) => result;
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
