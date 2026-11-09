using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class CatalogImportJobEndpointsTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private const string ImportJobsUrl =
        "/api/catalog/books/external/import-jobs";

    private readonly HttpClient _client;
    private readonly FavoBooksWebApplicationFactory _factory;

    public CatalogImportJobEndpointsTests(
        FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateImportJob_WithValidRequest_ReturnsAccepted()
    {
        // Arrange
        await AuthenticateAsync(_client);

        string query =
            $"The Hobbit {Guid.NewGuid():N}";

        var request = new
        {
            Provider = "OpenLibrary",
            Query = query,
            StartPage = 1,
            PageSize = 10,
            PageCount = 3
        };

        // Act
        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                ImportJobsUrl,
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Accepted,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Import işi oluşturma yanıtı boş döndü.");

        JsonElement root = responseBody.RootElement;

        Guid jobId =
            root.GetProperty("jobId").GetGuid();

        string expectedStatusUrl =
            $"{ImportJobsUrl}/{jobId}";

        Assert.NotEqual(Guid.Empty, jobId);

        Assert.Equal(
            "Pending",
            root.GetProperty("status").GetString());

        Assert.Equal(
            expectedStatusUrl,
            root.GetProperty("statusUrl").GetString());

        Assert.Equal(
            expectedStatusUrl,
            response.Headers.Location?.OriginalString);

        Assert.NotEqual(
            default,
            root.GetProperty("createdAtUtc")
                .GetDateTimeOffset());

        using HttpResponseMessage getResponse =
            await _client.GetAsync(expectedStatusUrl);

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using JsonDocument getResponseBody =
            await getResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Import işi durum yanıtı boş döndü.");

        JsonElement job = getResponseBody.RootElement;

        Assert.Equal(
            jobId,
            job.GetProperty("jobId").GetGuid());

        Assert.Equal(
            "OpenLibrary",
            job.GetProperty("provider").GetString());

        Assert.Equal(
            query,
            job.GetProperty("query").GetString());

        Assert.Equal(
            "Pending",
            job.GetProperty("status").GetString());

        Assert.Equal(
            1,
            job.GetProperty("startPage").GetInt32());

        Assert.Equal(
            1,
            job.GetProperty("nextPage").GetInt32());

        Assert.Equal(
            10,
            job.GetProperty("pageSize").GetInt32());

        Assert.Equal(
            3,
            job.GetProperty("pageCount").GetInt32());

        Assert.Equal(
            0,
            job.GetProperty("completedPageCount")
                .GetInt32());

        Assert.Equal(
            0,
            job.GetProperty("processedCount")
                .GetInt32());
    }

    [Fact]
    public async Task CreateImportJob_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync(_client);

        var request = new
        {
            Provider = "UnsupportedProvider",
            Query = " ",
            StartPage = 0,
            PageSize = 21,
            PageCount = 0
        };

        // Act
        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                ImportJobsUrl,
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Import doğrulama yanıtı boş döndü.");

        JsonElement errors =
            responseBody.RootElement
                .GetProperty("errors");

        Assert.Equal(
            "Yalnızca OpenLibrary sağlayıcısı " +
            "desteklenmektedir.",
            errors.GetProperty("provider")[0]
                .GetString());

        Assert.Equal(
            "Arama metni zorunludur.",
            errors.GetProperty("query")[0]
                .GetString());

        Assert.Equal(
            "Başlangıç sayfası en az 1 olmalıdır.",
            errors.GetProperty("startPage")[0]
                .GetString());

        Assert.Equal(
            "Sayfa büyüklüğü 1 ile 20 " +
            "arasında olmalıdır.",
            errors.GetProperty("pageSize")[0]
                .GetString());

        Assert.Equal(
            "Sayfa sayısı 1 ile 500 " +
            "arasında olmalıdır.",
            errors.GetProperty("pageCount")[0]
                .GetString());
    }

    [Fact]
    public async Task CreateImportJob_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Provider = "OpenLibrary",
            Query = "The Hobbit",
            StartPage = 1,
            PageSize = 10,
            PageCount = 1
        };

        // Act
        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                ImportJobsUrl,
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetImportJob_WhenJobDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid jobId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                $"{ImportJobsUrl}/{jobId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetImportJob_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        Guid jobId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                $"{ImportJobsUrl}/{jobId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private async Task AuthenticateAsync(
        HttpClient client)
    {
        string email =
            $"catalog-import-{Guid.NewGuid():N}" +
            "@kitapkivo.local";

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
}