using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Favo.Books.Api.IntegrationTests.Catalog;

public sealed class CatalogAdminAuthorizationTests(FavoBooksWebApplicationFactory factory)
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    public static TheoryData<string> ProtectedPosts =>
    [
        "/api/catalog/books/external/import-jobs",
        "/api/catalog/books/external/resolve",
        "/api/catalog/authors"
    ];

    [Theory]
    [MemberData(nameof(ProtectedPosts))]
    public async Task Post_WithoutToken_ReturnsUnauthorized(string path)
    {
        using HttpResponseMessage response = await factory.CreateClient().PostAsJsonAsync(path, new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedPosts))]
    public async Task Post_AsRegularUser_ReturnsForbidden(string path)
    {
        HttpClient client = await CreateSignedInClient(admin: false);

        using HttpResponseMessage response = await client.PostAsJsonAsync(path, new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedPosts))]
    public async Task Post_AsAdmin_PassesAuthorization(string path)
    {
        HttpClient client = await CreateSignedInClient(admin: true);

        using HttpResponseMessage response = await client.PostAsJsonAsync(path, new { });

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetImportJob_AsRegularUser_ReturnsForbidden()
    {
        HttpClient client = await CreateSignedInClient(admin: false);

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/catalog/books/external/import-jobs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> CreateSignedInClient(bool admin)
    {
        HttpClient client = factory.CreateClient();
        string email = $"catalog-admin-{Guid.NewGuid():N}@example.test";
        var credentials = new
        {
            Nickname = "reader_" + Guid.NewGuid().ToString("N")[..16],
            Email = email,
            Password = "Test1234!"
        };

        (await client.PostAsJsonAsync("/api/identity/register", credentials)).EnsureSuccessStatusCode();
        if (admin)
        {
            await factory.PromoteToAdminAsync(email);
        }

        HttpResponseMessage login = await client.PostAsJsonAsync("/api/identity/login", credentials);
        login.EnsureSuccessStatusCode();
        using JsonDocument body = (await login.Content.ReadFromJsonAsync<JsonDocument>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            body.RootElement.GetProperty("accessToken").GetString());

        return client;
    }
}
