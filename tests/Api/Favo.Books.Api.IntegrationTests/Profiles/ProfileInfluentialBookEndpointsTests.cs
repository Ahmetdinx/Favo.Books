using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Favo.Books.Api.IntegrationTests
    .Profiles;

public sealed class
    ProfileInfluentialBookEndpointsTests :
        IClassFixture<
            FavoBooksWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FavoBooksWebApplicationFactory _factory;

    public ProfileInfluentialBookEndpointsTests(
        FavoBooksWebApplicationFactory
            factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetInfluentialBook_WithMissingCatalogBook_DoesNotChangeSelection(
        bool hasSelection)
    {
        await AuthenticateAsync(_client);
        if (hasSelection)
        {
            Guid bookId = await CatalogTestData.CreateBookAsync(_factory);
            await SetInfluentialBookAsync(_client, bookId, 1, "Keep this note");
        }

        using HttpResponseMessage before = await _client.GetAsync("/api/profiles/me/influential-books");
        string previousSelection = await before.Content.ReadAsStringAsync();

        using HttpResponseMessage response = await _client.PutAsJsonAsync(
            "/api/profiles/me/influential-books/1",
            new { BookId = Guid.NewGuid(), PersonalNote = "Invalid", PersonalReflection = "Invalid" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument problem = (await response.Content.ReadFromJsonAsync<JsonDocument>())!;
        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Kitap bulunamadı.", problem.RootElement.GetProperty("title").GetString());

        using HttpResponseMessage after = await _client.GetAsync("/api/profiles/me/influential-books");
        Assert.Equal(before.StatusCode, after.StatusCode);
        Assert.Equal(previousSelection, await after.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task
        GetInfluentialBooks_ReturnsBooksOrderedByPosition()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid thirdBookId = await CatalogTestData.CreateBookAsync(_factory);
        Guid firstBookId = await CatalogTestData.CreateBookAsync(_factory);
        Guid fifthBookId = await CatalogTestData.CreateBookAsync(_factory);

        await SetInfluentialBookAsync(
            _client,
            thirdBookId,
            3,
            "Üçüncü sıradaki kitabım.");

        await SetInfluentialBookAsync(
            _client,
            firstBookId,
            1,
            "  Beni en çok etkileyen " +
            "kitap.  ");

        await SetInfluentialBookAsync(
            _client,
            fifthBookId,
            5,
            null);

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/profiles/me/" +
                "influential-books");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Etkileyen kitaplar " +
                    "yanıtının gövdesi boş " +
                    "döndü.");

        JsonElement items =
            responseBody.RootElement
                .GetProperty("items");

        Assert.Equal(
            3,
            items.GetArrayLength());

        Assert.Equal(
            1,
            items[0]
                .GetProperty("position")
                .GetInt32());

        Assert.Equal(
            firstBookId,
            items[0]
                .GetProperty("bookId")
                .GetGuid());

        Assert.Equal(
            "Beni en çok etkileyen kitap.",
            items[0]
                .GetProperty(
                    "personalReflection")
                .GetString());

        Assert.Equal(
            3,
            items[1]
                .GetProperty("position")
                .GetInt32());

        Assert.Equal(
            thirdBookId,
            items[1]
                .GetProperty("bookId")
                .GetGuid());

        Assert.Equal(
            5,
            items[2]
                .GetProperty("position")
                .GetInt32());

        Assert.Equal(
            fifthBookId,
            items[2]
                .GetProperty("bookId")
                .GetGuid());
    }

    [Fact]
    public async Task
        SetInfluentialBook_WhenPositionExists_UpdatesSameRecord()
    {
        // Arrange
        await AuthenticateAsync(_client);

        InfluentialBookTestResponse
            firstResult =
                await SetInfluentialBookAsync(
                    _client,
                    await CatalogTestData.CreateBookAsync(_factory),
                    2,
                    "İlk kitap.");

        Guid secondBookId =
            await CatalogTestData.CreateBookAsync(_factory);

        // Act
        InfluentialBookTestResponse
            secondResult =
                await SetInfluentialBookAsync(
                    _client,
                    secondBookId,
                    2,
                    "Yeni kitap.");

        // Assert
        Assert.Equal(
            firstResult.InfluentialBookId,
            secondResult
                .InfluentialBookId);

        Assert.Equal(
            secondBookId,
            secondResult.BookId);

        Assert.Equal(
            2,
            secondResult.Position);

        Assert.Equal(
            "Yeni kitap.",
            secondResult
                .PersonalReflection);
    }

    [Fact]
    public async Task
        SetInfluentialBook_WhenBookExistsAtAnotherPosition_ReturnsConflict()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid bookId = await CatalogTestData.CreateBookAsync(_factory);

        await SetInfluentialBookAsync(
            _client,
            bookId,
            1,
            "Birinci pozisyon.");

        var request = new
        {
            BookId = bookId,
            PersonalReflection =
                "İkinci pozisyon."
        };

        // Act
        using HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/profiles/me/" +
                "influential-books/2",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Çakışma yanıtının " +
                    "gövdesi boş döndü.");

        Assert.Equal(
            "Bu kitap etkileyen kitaplar " +
            "arasında zaten bulunuyor.",
            responseBody.RootElement
                .GetProperty("message")
                .GetString());
    }

    [Fact]
    public async Task
        SetInfluentialBook_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync(_client);

        var request = new
        {
            BookId = Guid.Empty,
            PersonalReflection =
                new string(
                    'a',
                    1001)
        };

        // Act
        using HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/profiles/me/" +
                "influential-books/6",
                request);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Doğrulama yanıtının " +
                    "gövdesi boş döndü.");

        JsonElement errors =
            responseBody.RootElement
                .GetProperty("errors");

        Assert.Equal(
            "Pozisyon 1 ile 5 arasında " +
            "olmalıdır.",
            errors
                .GetProperty("position")[0]
                .GetString());

        Assert.Equal(
            "Kitap kimliği boş olamaz.",
            errors
                .GetProperty("bookId")[0]
                .GetString());

        Assert.Equal(
            "Kişisel düşünce en fazla " +
            "1000 karakter olabilir.",
            errors
                .GetProperty(
                    "personalReflection")[0]
                .GetString());
    }

    [Fact]
    public async Task
        RemoveInfluentialBook_WhenPositionExists_RemovesBook()
    {
        // Arrange
        await AuthenticateAsync(_client);

        await SetInfluentialBookAsync(
            _client,
            await CatalogTestData.CreateBookAsync(_factory),
            4,
            "Silinecek kitap.");

        // Act
        using HttpResponseMessage
            deleteResponse =
                await _client.DeleteAsync(
                    "/api/profiles/me/" +
                    "influential-books/4");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        using HttpResponseMessage getResponse =
            await _client.GetAsync(
                "/api/profiles/me/" +
                "influential-books");

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using JsonDocument responseBody =
            await getResponse.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Etkileyen kitaplar " +
                    "yanıtının gövdesi boş " +
                    "döndü.");

        Assert.Equal(
            0,
            responseBody.RootElement
                .GetProperty("items")
                .GetArrayLength());
    }

    [Fact]
    public async Task
        GetInfluentialBooks_ReturnsOnlyAuthenticatedUsersBooks()
    {
        // Arrange
        await AuthenticateAsync(_client);

        await SetInfluentialBookAsync(
            _client,
            await CatalogTestData.CreateBookAsync(_factory),
            1,
            "Birinci kullanıcının kitabı.");

        await AuthenticateAsync(_client);

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/profiles/me/" +
                "influential-books");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Etkileyen kitaplar " +
                    "yanıtının gövdesi boş " +
                    "döndü.");

        Assert.Equal(
            0,
            responseBody.RootElement
                .GetProperty("items")
                .GetArrayLength());
    }

    [Fact]
    public async Task
        GetInfluentialBooks_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/profiles/me/" +
                "influential-books");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static async Task<
        InfluentialBookTestResponse>
        SetInfluentialBookAsync(
            HttpClient client,
            Guid bookId,
            int position,
            string? personalReflection)
    {
        var request = new
        {
            BookId = bookId,
            PersonalReflection =
                personalReflection
        };

        using HttpResponseMessage response =
            await client.PutAsJsonAsync(
                "/api/profiles/me/" +
                $"influential-books/{position}",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        using JsonDocument responseBody =
            await response.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Etkileyen kitap ayarlama " +
                    "yanıtının gövdesi boş " +
                    "döndü.");

        JsonElement root =
            responseBody.RootElement;

        return new InfluentialBookTestResponse(
            root
                .GetProperty(
                    "influentialBookId")
                .GetGuid(),
            root
                .GetProperty("bookId")
                .GetGuid(),
            root
                .GetProperty("position")
                .GetInt32(),
            root
                .GetProperty(
                    "personalReflection")
                .GetString());
    }

    private static async Task
        AuthenticateAsync(
            HttpClient client)
    {
        string email =
            $"profile-influential-" +
            $"{Guid.NewGuid():N}" +
            "@kitapkivo.local";

        const string password =
            "Test1234!";

        var credentials = new
        {
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16], Email = email,
            Password = password
        };

        using HttpResponseMessage
            registerResponse =
                await client.PostAsJsonAsync(
                    "/api/identity/register",
                    credentials);

        Assert.Equal(
            HttpStatusCode.Created,
            registerResponse.StatusCode);

        using HttpResponseMessage
            loginResponse =
                await client.PostAsJsonAsync(
                    "/api/identity/login",
                    credentials);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using JsonDocument loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Login yanıtının gövdesi " +
                    "boş döndü.");

        string accessToken =
            loginBody.RootElement
                .GetProperty("accessToken")
                .GetString()
            ?? throw new
                InvalidOperationException(
                    "Login yanıtında access " +
                    "token bulunamadı.");

        client.DefaultRequestHeaders
            .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
    }

    private sealed record
        InfluentialBookTestResponse(
            Guid InfluentialBookId,
            Guid BookId,
            int Position,
            string? PersonalReflection);
}
