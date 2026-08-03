using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
}