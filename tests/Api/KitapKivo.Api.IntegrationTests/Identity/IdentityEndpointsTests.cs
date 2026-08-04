using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;

namespace KitapKivo.Api.IntegrationTests.Identity;

public sealed class IdentityEndpointsTests
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IdentityEndpointsTests(
        KitapKivoWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreated()
    {
        var request = new
        {
            Email = $"integration-{Guid.NewGuid():N}@kitapkivo.local",
            Password = "Test1234!"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/identity/register",
            request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        JsonDocument responseBody =
        await response.Content.ReadFromJsonAsync<JsonDocument>()
        ?? throw new InvalidOperationException(
            "Register yanıtının gövdesi boş döndü.");

        Assert.True(responseBody.RootElement.TryGetProperty(
            "userId",
            out JsonElement userIdElement));

        Assert.True(Guid.TryParse(
            userIdElement.GetString(),
            out Guid userId));

        Assert.NotEqual(Guid.Empty, userId);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var request = new
        {
            Email = $"integration-{Guid.NewGuid():N}@kitapkivo.local",
            Password = "Test1234!"
        };

        using HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        using HttpResponseMessage secondResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                request);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndAccessToken()
    {
        string email =
            $"integration-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        var registerRequest = new
        {
            Email = email,
            Password = password
        };

        using HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                registerRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        using HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using JsonDocument responseBody =
            await loginResponse.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Login yanıtının gövdesi boş döndü.");

        Assert.True(responseBody.RootElement.TryGetProperty(
            "accessToken",
            out JsonElement accessTokenElement));

        string? accessToken = accessTokenElement.GetString();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        string email =
            $"integration-{Guid.NewGuid():N}@kitapkivo.local";

        var registerRequest = new
        {
            Email = email,
            Password = "Test1234!"
        };

        using HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                registerRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginRequest = new
        {
            Email = email,
            Password = "YanlisParola123!"
        };

        using HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithoutAccessToken_ReturnsUnauthorized()
    {
        using HttpResponseMessage response =
            await _client.GetAsync("/api/identity/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidAccessToken_ReturnsCurrentUser()
    {
        string email =
            $"integration-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        var registerRequest = new
        {
            Email = email,
            Password = password
        };

        using HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                registerRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        using HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using JsonDocument loginBody =
            await loginResponse.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Login yanıtının gövdesi boş döndü.");

        Assert.True(loginBody.RootElement.TryGetProperty(
            "accessToken",
            out JsonElement accessTokenElement));

        string accessToken =
            accessTokenElement.GetString()
            ?? throw new InvalidOperationException(
                "Login yanıtında access token bulunamadı.");

        using HttpRequestMessage request =
            new(HttpMethod.Get, "/api/identity/me");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        using HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Me yanıtının gövdesi boş döndü.");

        Assert.Equal(
            email,
            responseBody.RootElement
                .GetProperty("email")
                .GetString());
    }
}

