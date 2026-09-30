using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using Favo.Books.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Favo.Books.Api.IntegrationTests.Identity;

public sealed class IdentityEndpointsTests
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FavoBooksWebApplicationFactory _factory;

    public IdentityEndpointsTests(
       FavoBooksWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreated()
    {
        var request = new
        {
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = $"integration-{Guid.NewGuid():N}@kitapkivo.local",
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
    public async Task Login_WithValidCredentials_SetsRefreshTokenCookieAndStoresOnlyHash()
    {
        string email =
            $"refresh-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        var registerRequest = new
        {
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
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
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
            Password = password
        };

        using HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        string setCookieHeader =
            loginResponse.Headers
                .GetValues("Set-Cookie")
                .Single(header =>
                    header.StartsWith(
                        "KitapKivo.RefreshToken=",
                        StringComparison.Ordinal));

        string normalizedCookieHeader =
            setCookieHeader.ToLowerInvariant();

        Assert.Contains(
            "; httponly",
            normalizedCookieHeader);

        Assert.Contains(
            "; secure",
            normalizedCookieHeader);

        Assert.Contains(
            "samesite=lax",
            normalizedCookieHeader);

        Assert.Contains(
            "path=/api/identity",
            normalizedCookieHeader);

        int separatorIndex =
            setCookieHeader.IndexOf(';');

        string cookiePair =
            separatorIndex >= 0
                ? setCookieHeader[..separatorIndex]
                : setCookieHeader;

        int equalsIndex =
            cookiePair.IndexOf('=');

        Assert.True(equalsIndex > 0);

        string rawRefreshToken =
            cookiePair[(equalsIndex + 1)..];

        Assert.False(
            string.IsNullOrWhiteSpace(rawRefreshToken));

        string expectedTokenHash =
            Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        rawRefreshToken)));

        using IServiceScope scope =
            _factory.Services.CreateScope();

        IdentityDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                IdentityDbContext>();

        Guid userId = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();

        var storedRefreshToken =
            await dbContext.RefreshTokens
                .AsNoTracking()
                .SingleAsync(token =>
                    token.UserId == userId);

        Assert.Equal(
            expectedTokenHash,
            storedRefreshToken.TokenHash);

        Assert.NotEqual(
            rawRefreshToken,
            storedRefreshToken.TokenHash);

        Assert.Null(
            storedRefreshToken.RevokedAtUtc);

        Assert.Null(
            storedRefreshToken.ReplacedByTokenId);

        Assert.True(
            storedRefreshToken.ExpiresAtUtc >
            storedRefreshToken.CreatedAtUtc);

        using JsonDocument responseBody =
            await loginResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Login yanıtının gövdesi boş döndü.");

        Assert.False(
            responseBody.RootElement.TryGetProperty(
                "refreshToken",
                out _));
    }

    [Fact]
    public async Task Refresh_WhenRotatedTokenIsReused_RevokesTokenFamily()
    {
        string email =
            $"replay-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        using HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                new
                {
                    Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        using HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/login",
                new
                {
                    Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        string originalToken =
            GetCookieValue(
                loginResponse,
                "KitapKivo.RefreshToken");

        using HttpRequestMessage firstRefreshRequest =
            CreateRefreshRequest(originalToken);

        using HttpResponseMessage firstRefreshResponse =
            await _client.SendAsync(
                firstRefreshRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            firstRefreshResponse.StatusCode);

        string replacementToken =
            GetCookieValue(
                firstRefreshResponse,
                "KitapKivo.RefreshToken");

        using HttpRequestMessage replayRequest =
            CreateRefreshRequest(originalToken);

        using HttpResponseMessage replayResponse =
            await _client.SendAsync(replayRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            replayResponse.StatusCode);

        string originalHash =
            HashRefreshToken(originalToken);

        string replacementHash =
            HashRefreshToken(replacementToken);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        IdentityDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                IdentityDbContext>();

        var tokenFamily =
            await dbContext.RefreshTokens
                .AsNoTracking()
                .Where(token =>
                    token.TokenHash == originalHash ||
                    token.TokenHash == replacementHash)
                .ToListAsync();

        Assert.Equal(2, tokenFamily.Count);

        Assert.All(
            tokenFamily,
            token => Assert.NotNull(
                token.RevokedAtUtc));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        using HttpRequestMessage request =
            new(HttpMethod.Post, "/api/identity/refresh");

        using HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesToken()
    {
        string email =
            $"rotation-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        using HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                new
                {
                    Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        using HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/login",
                new
                {
                    Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        string originalToken =
            GetCookieValue(
                loginResponse,
                "KitapKivo.RefreshToken");

        using HttpRequestMessage refreshRequest =
            new(HttpMethod.Post, "/api/identity/refresh");

        refreshRequest.Headers.TryAddWithoutValidation(
            "Cookie",
            $"KitapKivo.RefreshToken={originalToken}");

        using HttpResponseMessage refreshResponse =
            await _client.SendAsync(refreshRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            refreshResponse.StatusCode);

        string replacementToken =
            GetCookieValue(
                refreshResponse,
                "KitapKivo.RefreshToken");

        Assert.NotEqual(
            originalToken,
            replacementToken);

        using JsonDocument responseBody =
            await refreshResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Refresh yanıtının gövdesi boş döndü.");

        string? accessToken =
            responseBody.RootElement
                .GetProperty("accessToken")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(accessToken));

        string originalHash =
            HashRefreshToken(originalToken);

        string replacementHash =
            HashRefreshToken(replacementToken);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        IdentityDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                IdentityDbContext>();

        var storedTokens =
            await dbContext.RefreshTokens
                .AsNoTracking()
                .Where(token =>
                    token.TokenHash == originalHash ||
                    token.TokenHash == replacementHash)
                .ToListAsync();

        Assert.Equal(2, storedTokens.Count);

        var originalStoredToken =
            storedTokens.Single(token =>
                token.TokenHash == originalHash);

        var replacementStoredToken =
            storedTokens.Single(token =>
                token.TokenHash == replacementHash);

        Assert.NotNull(
            originalStoredToken.RevokedAtUtc);

        Assert.Equal(
            replacementStoredToken.Id,
            originalStoredToken.ReplacedByTokenId);

        Assert.Null(
            replacementStoredToken.RevokedAtUtc);

        Assert.Equal(
            originalStoredToken.FamilyId,
            replacementStoredToken.FamilyId);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var request = new
        {
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = $"integration-{Guid.NewGuid():N}@kitapkivo.local",
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
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
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
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
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
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
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
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
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
    public async Task Logout_WithoutCookie_ReturnsNoContent()
    {
        using HttpRequestMessage request =
            new(HttpMethod.Post, "/api/identity/logout");

        using HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        string setCookieHeader =
            response.Headers
                .GetValues("Set-Cookie")
                .Single(header =>
                    header.StartsWith(
                        "KitapKivo.RefreshToken=",
                        StringComparison.Ordinal));

        Assert.Contains(
            "expires=thu, 01 jan 1970",
            setCookieHeader.ToLowerInvariant());
    }

    [Fact]
    public async Task Logout_WithValidToken_RevokesTokenAndClearsCookie()
    {
        string email =
            $"logout-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        using HttpResponseMessage registerResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/register",
                new
                {
                    Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        using HttpResponseMessage loginResponse =
            await _client.PostAsJsonAsync(
                "/api/identity/login",
                new
                {
                    Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
                    Password = password
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        string refreshToken =
            GetCookieValue(
                loginResponse,
                "KitapKivo.RefreshToken");

        using HttpRequestMessage logoutRequest =
            new(HttpMethod.Post, "/api/identity/logout");

        logoutRequest.Headers.TryAddWithoutValidation(
            "Cookie",
            $"KitapKivo.RefreshToken={refreshToken}");

        using HttpResponseMessage logoutResponse =
            await _client.SendAsync(logoutRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        string tokenHash =
            HashRefreshToken(refreshToken);

        using (IServiceScope scope =
               _factory.Services.CreateScope())
        {
            IdentityDbContext dbContext =
                scope.ServiceProvider.GetRequiredService<
                    IdentityDbContext>();

            var storedToken =
                await dbContext.RefreshTokens
                    .AsNoTracking()
                    .SingleAsync(token =>
                        token.TokenHash == tokenHash);

            Assert.NotNull(
                storedToken.RevokedAtUtc);
        }

        using HttpRequestMessage refreshRequest =
            CreateRefreshRequest(refreshToken);

        using HttpResponseMessage refreshResponse =
            await _client.SendAsync(refreshRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidAccessToken_ReturnsCurrentUser()
    {
        string email =
            $"integration-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        var registerRequest = new
        {
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
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
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
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
    private static string GetCookieValue(
    HttpResponseMessage response,
    string cookieName)
    {
        string prefix = $"{cookieName}=";

        string setCookieHeader =
            response.Headers
                .GetValues("Set-Cookie")
                .Single(header =>
                    header.StartsWith(
                        prefix,
                        StringComparison.Ordinal));

        int endIndex =
            setCookieHeader.IndexOf(';');

        return endIndex >= 0
            ? setCookieHeader[
                prefix.Length..endIndex]
            : setCookieHeader[prefix.Length..];
    }

    private static string HashRefreshToken(
        string refreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    refreshToken)));
    }

    private static HttpRequestMessage CreateRefreshRequest(
    string refreshToken)
    {
        HttpRequestMessage request =
            new(
                HttpMethod.Post,
                "/api/identity/refresh");

        request.Headers.TryAddWithoutValidation(
            "Cookie",
            $"KitapKivo.RefreshToken={refreshToken}");

        return request;
    }
}

