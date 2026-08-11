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

    [Fact]
    public async Task GetLibrary_WithFilterAndPagination_ReturnsMatchingEntries()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid firstReadingBookId = Guid.NewGuid();
        Guid secondReadingBookId = Guid.NewGuid();

        await AddBookAsync(
            _client,
            firstReadingBookId,
            "Reading");

        await AddBookAsync(
            _client,
            Guid.NewGuid(),
            "WantToRead");

        await AddBookAsync(
            _client,
            secondReadingBookId,
            "Reading");

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/reading/library" +
                "?status=Reading&page=1&pageSize=1");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kütüphane yanıtının gövdesi boş döndü.");

        JsonElement root = responseBody.RootElement;
        JsonElement items = root.GetProperty("items");

        Assert.Equal(2, root.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(
            "Reading",
            items[0].GetProperty("status").GetString());

        Guid returnedBookId =
            items[0].GetProperty("bookId").GetGuid();

        Assert.Contains(
            returnedBookId,
            new[]
            {
            firstReadingBookId,
            secondReadingBookId
            });
    }

    [Fact]
    public async Task GetLibrary_WithInvalidQuery_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync(_client);

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/reading/library" +
                "?status=Finished&page=0&pageSize=101");

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
            "Durum WantToRead, Reading, Read, " +
            "OnHold veya Dropped olmalıdır.",
            errors.GetProperty("status")[0].GetString());

        Assert.Equal(
            "Sayfa numarası en az 1 olmalıdır.",
            errors.GetProperty("page")[0].GetString());

        Assert.Equal(
            "Sayfa boyutu 1 ile 100 arasında olmalıdır.",
            errors.GetProperty("pageSize")[0].GetString());
    }

    [Fact]
    public async Task GetLibrary_ReturnsOnlyAuthenticatedUsersEntries()
    {
        // Arrange
        await AuthenticateAsync(_client);

        await AddBookAsync(
            _client,
            Guid.NewGuid(),
            "Read");

        await AuthenticateAsync(_client);

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/reading/library");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kütüphane yanıtının gövdesi boş döndü.");

        Assert.Equal(
            0,
            responseBody.RootElement
                .GetProperty("totalCount")
                .GetInt32());

        Assert.Equal(
            0,
            responseBody.RootElement
                .GetProperty("items")
                .GetArrayLength());
    }

    [Fact]
    public async Task GetLibrary_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/reading/library");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task UpdateLibraryStatus_WithValidRequest_ReturnsOkAndPersistsChange()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid libraryEntryId =
            await AddBookAsync(
                _client,
                Guid.NewGuid(),
                "WantToRead");

        var request = new
        {
            Status = "Reading"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/reading/library/{libraryEntryId}/status",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Durum güncelleme yanıtının gövdesi boş döndü.");

        JsonElement root = responseBody.RootElement;

        Assert.Equal(
            libraryEntryId,
            root.GetProperty("libraryEntryId").GetGuid());

        Assert.Equal(
            "Reading",
            root.GetProperty("status").GetString());

        Assert.NotEqual(
            default(DateTimeOffset),
            root.GetProperty("updatedAtUtc").GetDateTimeOffset());

        using HttpResponseMessage getResponse =
            await _client.GetAsync(
                "/api/reading/library" +
                "?status=Reading&page=1&pageSize=100");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using JsonDocument getResponseBody =
            await getResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kütüphane yanıtının gövdesi boş döndü.");

        bool containsUpdatedEntry =
            getResponseBody.RootElement
                .GetProperty("items")
                .EnumerateArray()
                .Any(item =>
                    item.GetProperty("libraryEntryId").GetGuid() ==
                    libraryEntryId);

        Assert.True(containsUpdatedEntry);
    }

    [Fact]
    public async Task UpdateLibraryStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid libraryEntryId =
            await AddBookAsync(
                _client,
                Guid.NewGuid(),
                "WantToRead");

        var request = new
        {
            Status = "Finished"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/reading/library/{libraryEntryId}/status",
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
            "Durum WantToRead, Reading, Read, " +
            "OnHold veya Dropped olmalıdır.",
            errors.GetProperty("status")[0].GetString());
    }

    [Fact]
    public async Task UpdateLibraryStatus_WhenEntryDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync(_client);

        var request = new
        {
            Status = "Reading"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/reading/library/{Guid.NewGuid()}/status",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Bulunamadı yanıtının gövdesi boş döndü.");

        Assert.Equal(
            "Kütüphane kaydı bulunamadı.",
            responseBody.RootElement
                .GetProperty("message")
                .GetString());
    }

    [Fact]
    public async Task UpdateLibraryStatus_WhenEntryBelongsToAnotherUser_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid libraryEntryId =
            await AddBookAsync(
                _client,
                Guid.NewGuid(),
                "WantToRead");

        await AuthenticateAsync(_client);

        var request = new
        {
            Status = "Reading"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/reading/library/{libraryEntryId}/status",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateLibraryStatus_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Status = "Reading"
        };

        // Act
        using HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/reading/library/{Guid.NewGuid()}/status",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static async Task<Guid> AddBookAsync(
      HttpClient client,
      Guid bookId,
      string status)
    {
        var request = new
        {
            BookId = bookId,
            Status = status
        };

        using HttpResponseMessage response =
            await client.PostAsJsonAsync(
                "/api/reading/library",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kütüphaneye ekleme yanıtının gövdesi boş döndü.");

        return responseBody.RootElement
            .GetProperty("libraryEntryId")
            .GetGuid();
    }
    [Fact]
    public async Task RemoveBookFromLibrary_WhenEntryExists_ReturnsNoContentAndRemovesEntry()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid libraryEntryId =
            await AddBookAsync(
                _client,
                Guid.NewGuid(),
                "WantToRead");

        // Act
        using HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/reading/library/{libraryEntryId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        using HttpResponseMessage getResponse =
            await _client.GetAsync(
                "/api/reading/library?page=1&pageSize=100");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using JsonDocument responseBody =
            await getResponse.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Kütüphane yanıtının gövdesi boş döndü.");

        bool containsRemovedEntry =
            responseBody.RootElement
                .GetProperty("items")
                .EnumerateArray()
                .Any(item =>
                    item.GetProperty("libraryEntryId").GetGuid() ==
                    libraryEntryId);

        Assert.False(containsRemovedEntry);
    }

    [Fact]
    public async Task RemoveBookFromLibrary_WhenEntryDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync(_client);

        // Act
        using HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/reading/library/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException(
                "Bulunamadı yanıtının gövdesi boş döndü.");

        Assert.Equal(
            "Kütüphane kaydı bulunamadı.",
            responseBody.RootElement
                .GetProperty("message")
                .GetString());
    }

    [Fact]
    public async Task RemoveBookFromLibrary_WhenEntryBelongsToAnotherUser_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid libraryEntryId =
            await AddBookAsync(
                _client,
                Guid.NewGuid(),
                "WantToRead");

        await AuthenticateAsync(_client);

        // Act
        using HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/reading/library/{libraryEntryId}");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task RemoveBookFromLibrary_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Act
        using HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/reading/library/{Guid.NewGuid()}");

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