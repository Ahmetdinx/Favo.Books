using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KitapKivo.Api.IntegrationTests
    .Profiles;

public sealed class
    ProfileBedsideBookEndpointsTests :
        IClassFixture<
            KitapKivoWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly KitapKivoWebApplicationFactory _factory;

    public ProfileBedsideBookEndpointsTests(
        KitapKivoWebApplicationFactory
            factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetBedsideBook_WithMissingCatalogBook_DoesNotChangeSelection(
        bool hasSelection)
    {
        await AuthenticateAsync(_client);
        if (hasSelection)
        {
            Guid bookId = await CatalogTestData.CreateBookAsync(_factory);
            await SetBedsideBookAsync(_client, bookId, "Keep this note");
        }

        using HttpResponseMessage before = await _client.GetAsync("/api/profiles/me/bedside-book");
        string previousSelection = await before.Content.ReadAsStringAsync();

        using HttpResponseMessage response = await _client.PutAsJsonAsync(
            "/api/profiles/me/bedside-book",
            new { BookId = Guid.NewGuid(), PersonalNote = "Invalid", PersonalReflection = "Invalid" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument problem = (await response.Content.ReadFromJsonAsync<JsonDocument>())!;
        Assert.Equal(404, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Kitap bulunamadı.", problem.RootElement.GetProperty("title").GetString());

        using HttpResponseMessage after = await _client.GetAsync("/api/profiles/me/bedside-book");
        Assert.Equal(before.StatusCode, after.StatusCode);
        if (hasSelection)
        {
            Assert.Equal(previousSelection, await after.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task
        SetBedsideBook_WithValidRequest_PersistsAndReturnsBook()
    {
        // Arrange
        await AuthenticateAsync(_client);

        Guid bookId = await CatalogTestData.CreateBookAsync(_factory);

        // Act
        BedsideBookTestResponse setResult =
            await SetBedsideBookAsync(
                _client,
                bookId,
                "  Her okumada bana yeni " +
                "bir şey söylüyor.  ");

        using HttpResponseMessage getResponse =
            await _client.GetAsync(
                "/api/profiles/me/bedside-book");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        using JsonDocument responseBody =
            await getResponse.Content
                .ReadFromJsonAsync<
                    JsonDocument>()
            ?? throw new
                InvalidOperationException(
                    "Başucu kitabı yanıtının " +
                    "gövdesi boş döndü.");

        JsonElement root =
            responseBody.RootElement;

        Assert.Equal(
            setResult.BedsideBookId,
            root
                .GetProperty(
                    "bedsideBookId")
                .GetGuid());

        Assert.Equal(
            bookId,
            root.GetProperty("bookId")
                .GetGuid());

        Assert.Equal(
            "Her okumada bana yeni " +
            "bir şey söylüyor.",
            root
                .GetProperty(
                    "personalNote")
                .GetString());

        Assert.NotEqual(
            default(DateTimeOffset),
            root
                .GetProperty(
                    "createdAtUtc")
                .GetDateTimeOffset());

        Assert.NotEqual(
            default(DateTimeOffset),
            root
                .GetProperty(
                    "updatedAtUtc")
                .GetDateTimeOffset());
    }

    [Fact]
    public async Task
        SetBedsideBook_WhenSelectionExists_UpdatesSameRecord()
    {
        // Arrange
        await AuthenticateAsync(_client);

        BedsideBookTestResponse firstResult =
            await SetBedsideBookAsync(
                _client,
                await CatalogTestData.CreateBookAsync(_factory),
                "İlk başucu kitabım.");

        Guid secondBookId =
            await CatalogTestData.CreateBookAsync(_factory);

        // Act
        BedsideBookTestResponse secondResult =
            await SetBedsideBookAsync(
                _client,
                secondBookId,
                "Yeni başucu kitabım.");

        // Assert
        Assert.Equal(
            firstResult.BedsideBookId,
            secondResult.BedsideBookId);

        Assert.Equal(
            secondBookId,
            secondResult.BookId);

        Assert.Equal(
            "Yeni başucu kitabım.",
            secondResult.PersonalNote);
    }

    [Fact]
    public async Task
        SetBedsideBook_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsync(_client);

        var request = new
        {
            BookId = Guid.Empty,
            PersonalNote = new string(
                'a',
                501)
        };

        // Act
        using HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/profiles/me/bedside-book",
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
            "Kitap kimliği boş olamaz.",
            errors
                .GetProperty("bookId")[0]
                .GetString());

        Assert.Equal(
            "Kişisel not en fazla 500 " +
            "karakter olabilir.",
            errors
                .GetProperty(
                    "personalNote")[0]
                .GetString());
    }

    [Fact]
    public async Task
        RemoveBedsideBook_WhenSelectionExists_RemovesBook()
    {
        // Arrange
        await AuthenticateAsync(_client);

        await SetBedsideBookAsync(
            _client,
            await CatalogTestData.CreateBookAsync(_factory),
            "Silinecek başucu kitabı.");

        // Act
        using HttpResponseMessage
            deleteResponse =
                await _client.DeleteAsync(
                    "/api/profiles/me/" +
                    "bedside-book");

        // Assert
        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        using HttpResponseMessage getResponse =
            await _client.GetAsync(
                "/api/profiles/me/bedside-book");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task
        GetBedsideBook_WhenSelectionBelongsToAnotherUser_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync(_client);

        await SetBedsideBookAsync(
            _client,
            await CatalogTestData.CreateBookAsync(_factory),
            "Birinci kullanıcının kitabı.");

        await AuthenticateAsync(_client);

        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/profiles/me/bedside-book");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task
        GetBedsideBook_WithoutAccessToken_ReturnsUnauthorized()
    {
        // Act
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/profiles/me/bedside-book");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    private static async Task<
        BedsideBookTestResponse>
        SetBedsideBookAsync(
            HttpClient client,
            Guid bookId,
            string? personalNote)
    {
        var request = new
        {
            BookId = bookId,
            PersonalNote = personalNote
        };

        using HttpResponseMessage response =
            await client.PutAsJsonAsync(
                "/api/profiles/me/bedside-book",
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
                    "Başucu kitabı ayarlama " +
                    "yanıtının gövdesi boş döndü.");

        JsonElement root =
            responseBody.RootElement;

        return new BedsideBookTestResponse(
            root
                .GetProperty(
                    "bedsideBookId")
                .GetGuid(),
            root
                .GetProperty("bookId")
                .GetGuid(),
            root
                .GetProperty(
                    "personalNote")
                .GetString());
    }

    private static async Task
        AuthenticateAsync(
            HttpClient client)
    {
        string email =
            $"profile-{Guid.NewGuid():N}" +
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
        BedsideBookTestResponse(
            Guid BedsideBookId,
            Guid BookId,
            string? PersonalNote);
}
