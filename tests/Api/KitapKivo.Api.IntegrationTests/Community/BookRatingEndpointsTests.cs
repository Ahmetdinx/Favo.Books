using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KitapKivo.Modules.Community.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Api.IntegrationTests.Community;

public sealed class BookRatingEndpointsTests : IClassFixture<KitapKivoWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly KitapKivoWebApplicationFactory _factory;

    public BookRatingEndpointsTests(KitapKivoWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DetailedJourney_UpdatesAveragesAndOnlyRemovesOwnRating()
    {
        await AuthenticateAsync();
        Guid bookId = await CatalogTestData.CreateBookAsync(_client);
        using JsonDocument empty = await GetAsync(bookId);
        Assert.Equal(0, empty.RootElement.GetProperty("ratingCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, empty.RootElement.GetProperty("averageScore").ValueKind);
        Assert.Equal(JsonValueKind.Null, empty.RootElement.GetProperty("criteriaAverages").ValueKind);

        using JsonDocument created = await SetAsync(bookId, Scores(2));
        using JsonDocument updated = await SetAsync(bookId, new
        {
            Theme = 8, Language = 9, Characters = 7, Structure = 8, Pace = 6, Impact = 9, Experience = 8
        });
        Assert.Equal(1, updated.RootElement.GetProperty("ratingCount").GetInt32());
        Assert.Equal(7.95m, updated.RootElement.GetProperty("averageScore").GetDecimal());
        Assert.Equal(6, updated.RootElement.GetProperty("currentUserRating")
            .GetProperty("scores").GetProperty("pace").GetInt32());
        Assert.Equal(100, updated.RootElement.GetProperty("criteria")
            .EnumerateArray().Sum(criterion => criterion.GetProperty("weight").GetInt32()));

        await AuthenticateAsync();
        using JsonDocument second = await SetAsync(bookId, Scores(2));
        Assert.Equal(2, second.RootElement.GetProperty("ratingCount").GetInt32());
        Assert.Equal(4.975m, second.RootElement.GetProperty("averageScore").GetDecimal());
        Assert.Equal(4m, second.RootElement.GetProperty("criteriaAverages").GetProperty("pace").GetDecimal());
        Assert.Equal(5.5m, second.RootElement.GetProperty("criteriaAverages").GetProperty("language").GetDecimal());

        using HttpResponseMessage removed = await _client.DeleteAsync($"/api/community/books/{bookId}/rating");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        using JsonDocument remaining = await GetAsync(bookId);
        Assert.Equal(1, remaining.RootElement.GetProperty("ratingCount").GetInt32());
        Assert.Equal(7.95m, remaining.RootElement.GetProperty("averageScore").GetDecimal());
        Assert.Equal(JsonValueKind.Null, remaining.RootElement.GetProperty("currentUserRating").ValueKind);

        using HttpClient guest = _factory.CreateClient();
        using HttpResponseMessage publicResponse = await guest.GetAsync($"/api/community/books/{bookId}/ratings");
        Assert.True(publicResponse.Headers.CacheControl?.NoStore);
        using JsonDocument publicBody = (await publicResponse.Content.ReadFromJsonAsync<JsonDocument>())!;
        Assert.Equal(JsonValueKind.Null, publicBody.RootElement.GetProperty("currentUserRating").ValueKind);
    }

    [Theory]
    [InlineData("theme")]
    [InlineData("language")]
    [InlineData("characters")]
    [InlineData("structure")]
    [InlineData("pace")]
    [InlineData("impact")]
    [InlineData("experience")]
    public async Task MissingOrInvalidCriterion_Returns400AndPreservesExistingVote(string key)
    {
        await AuthenticateAsync();
        Guid bookId = await CatalogTestData.CreateBookAsync(_client);
        using JsonDocument original = await SetAsync(bookId, Scores(8));
        foreach (object? invalid in new object?[] { null, 0, 11, 2.5, "invalid" })
        {
            var scores = new Dictionary<string, object?>
            {
                ["theme"] = 5, ["language"] = 5, ["characters"] = 5, ["structure"] = 5,
                ["pace"] = 5, ["impact"] = 5, ["experience"] = 5
            };
            if (invalid is null) scores.Remove(key);
            else scores[key] = invalid;
            using HttpResponseMessage response = await _client.PutAsJsonAsync(
                $"/api/community/books/{bookId}/rating", scores);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using JsonDocument unchanged = await GetAsync(bookId);
        Assert.Equal(8m, unchanged.RootElement.GetProperty("averageScore").GetDecimal());
        Assert.Equal(1, unchanged.RootElement.GetProperty("ratingCount").GetInt32());
    }

    [Fact]
    public async Task LegacyPayload_IsRejectedAndSuppliedOverallCannotOverrideCalculation()
    {
        await AuthenticateAsync();
        Guid bookId = await CatalogTestData.CreateBookAsync(_client);
        using HttpResponseMessage old = await _client.PutAsJsonAsync(
            $"/api/community/books/{bookId}/rating", new { Score = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, old.StatusCode);
        using JsonDocument stored = await SetAsync(bookId, new
        {
            Theme = 2, Language = 2, Characters = 2, Structure = 2, Pace = 2, Impact = 2, Experience = 2,
            OverallScore = 10
        });
        Assert.Equal(2m, stored.RootElement.GetProperty("averageScore").GetDecimal());
    }

    [Fact]
    public async Task LegacyVote_IsExcludedUntilUserSuppliesAllCriteria()
    {
        Guid userId = await AuthenticateAsync();
        Guid bookId = await CatalogTestData.CreateBookAsync(_client);
        Guid ratingId = Guid.NewGuid();
        DateTimeOffset createdAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO community."BookRatings" ("Id", "UserId", "BookId", "LegacyScore", "CreatedAtUtc", "UpdatedAtUtc")
                VALUES ({ratingId}, {userId}, {bookId}, 5, {createdAt}, {createdAt})
                """);
        }

        using JsonDocument legacy = await GetAsync(bookId);
        Assert.Equal(0, legacy.RootElement.GetProperty("ratingCount").GetInt32());
        Assert.Equal(1, legacy.RootElement.GetProperty("legacyRatingCount").GetInt32());
        Assert.Equal(5, legacy.RootElement.GetProperty("currentUserLegacyScore").GetInt32());
        Assert.Equal(JsonValueKind.Null, legacy.RootElement.GetProperty("averageScore").ValueKind);

        using JsonDocument upgraded = await SetAsync(bookId, Scores(7));
        Assert.Equal(1, upgraded.RootElement.GetProperty("ratingCount").GetInt32());
        Assert.Equal(0, upgraded.RootElement.GetProperty("legacyRatingCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, upgraded.RootElement.GetProperty("currentUserLegacyScore").ValueKind);
        using IServiceScope verification = _factory.Services.CreateScope();
        var context = verification.ServiceProvider.GetRequiredService<CommunityDbContext>();
        var row = await context.BookRatings.SingleAsync(rating => rating.BookId == bookId);
        Assert.Equal(ratingId, row.Id);
        Assert.Equal(createdAt, row.CreatedAtUtc);
        Assert.Equal(7m, row.OverallScore);
    }

    [Fact]
    public async Task ConcurrentFirstVotes_KeepOneCompleteRow()
    {
        await AuthenticateAsync();
        Guid bookId = await CatalogTestData.CreateBookAsync(_client);
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(1, 6).Select(score =>
            _client.PutAsJsonAsync($"/api/community/books/{bookId}/rating", Scores(score))));
        foreach (HttpResponseMessage response in responses)
        {
            using (response) Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        using JsonDocument result = await GetAsync(bookId);
        Assert.Equal(1, result.RootElement.GetProperty("ratingCount").GetInt32());
        int own = result.RootElement.GetProperty("currentUserRating").GetProperty("scores").GetProperty("theme").GetInt32();
        Assert.Equal((decimal)own, result.RootElement.GetProperty("averageScore").GetDecimal());
    }

    [Fact]
    public async Task DeleteLastVote_RestoresNullAverages()
    {
        await AuthenticateAsync();
        Guid bookId = await CatalogTestData.CreateBookAsync(_client);
        using JsonDocument vote = await SetAsync(bookId, Scores(10));
        using HttpResponseMessage deleted = await _client.DeleteAsync($"/api/community/books/{bookId}/rating");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using JsonDocument result = await GetAsync(bookId);
        Assert.Equal(0, result.RootElement.GetProperty("ratingCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, result.RootElement.GetProperty("averageScore").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.RootElement.GetProperty("criteriaAverages").ValueKind);
    }

    [Fact]
    public async Task MissingBookAndAnonymousWrites_AreRejected()
    {
        using HttpResponseMessage anonymous = await _client.PutAsJsonAsync(
            $"/api/community/books/{Guid.NewGuid()}/rating", Scores(5));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        await AuthenticateAsync();
        using HttpResponseMessage missing = await _client.PutAsJsonAsync(
            $"/api/community/books/{Guid.NewGuid()}/rating", Scores(5));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private async Task<JsonDocument> SetAsync(Guid bookId, object scores)
    {
        using HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/community/books/{bookId}/rating", scores);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonDocument>())!;
    }

    private async Task<JsonDocument> GetAsync(Guid bookId)
    {
        using HttpResponseMessage response = await _client.GetAsync($"/api/community/books/{bookId}/ratings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonDocument>())!;
    }

    private static object Scores(int value) => new
    {
        Theme = value, Language = value, Characters = value, Structure = value,
        Pace = value, Impact = value, Experience = value
    };

    private async Task<Guid> AuthenticateAsync()
    {
        var credentials = new { Email = $"rating-{Guid.NewGuid():N}@kitapkivo.local", Password = "Test1234!" };
        using HttpResponseMessage register = await _client.PostAsJsonAsync("/api/identity/register", credentials);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using HttpResponseMessage login = await _client.PostAsJsonAsync("/api/identity/login", credentials);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using JsonDocument body = (await login.Content.ReadFromJsonAsync<JsonDocument>())!;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", body.RootElement.GetProperty("accessToken").GetString());
        return body.RootElement.GetProperty("userId").GetGuid();
    }
}
