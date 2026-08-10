using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KitapKivo.Api.IntegrationTests.Reading;

public sealed class ReadingLibraryEndpointsTests
    : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReadingLibraryEndpointsTests(
        KitapKivoWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddBookToLibrary_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid bookId = Guid.NewGuid();

        var request = new
        {
            BookId = bookId,
            Status = "WantToRead"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/reading/library",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kütüphaneye ekleme yanıtının gövdesi boş döndü.");

        Guid libraryEntryId =
            responseBody.RootElement
                .GetProperty("libraryEntryId")
                .GetGuid();

        Assert.NotEqual(Guid.Empty, libraryEntryId);

        Assert.Equal(
            $"/api/reading/library/{libraryEntryId}",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AddBookToLibrary_WhenBookAlreadyExists_ReturnsConflict()
    {
        // Arrange
        await AuthenticateAsync(_client);

        var request = new
        {
            BookId = Guid.NewGuid(),
            Status = "Reading"
        };

        using HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/reading/library",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        // Act
        using HttpResponseMessage secondResponse =
            await _client.PostAsJsonAsync(
                "/api/reading/library",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);

        using JsonDocument responseBody =
            await secondResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Çakışma yanıtının gövdesi boş döndü.");

        Assert.Equal(
            "Bu kitap kullanıcının kütüphanesinde zaten bulunuyor.",
            responseBody.RootElement
                .GetProperty("message")
                .GetString());
    }

    [Fact]
    public async Task AddBookToLibrary_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync(_client);

        var request = new
        {
            BookId = Guid.Empty,
            Status = "Finished"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/reading/library",
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
            "Kitap kimliği boş olamaz.",
            errors.GetProperty("bookId")[0].GetString());

        Assert.Equal(
            "Durum WantToRead, Reading, Read, " +
            "OnHold veya Dropped olmalıdır.",
            errors.GetProperty("status")[0].GetString());
    }

    [Fact]
    public async Task AddBookToLibrary_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            BookId = Guid.NewGuid(),
            Status = "WantToRead"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/reading/library",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static async Task AuthenticateAsync(
        HttpClient client)
    {
        string email =
            $"reading-{Guid.NewGuid():N}@kitapkivo.local";

        const string password = "Test1234!";

        var credentials = new
        {
            Email = email,
            Password = password
        };

        using HttpResponseMessage registerResponse =
            await client.PostAsJsonAsync(
                "/api/identity/register",
                credentials);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

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