using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Favo.Books.Modules.Catalog.Domain.Editions;
using Favo.Books.Modules.Catalog.Persistence;
using Favo.Books.Modules.Community.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Profiles;

public sealed class AchievementEndpointsTests(FavoBooksWebApplicationFactory factory) : IClassFixture<FavoBooksWebApplicationFactory>
{
    private async Task<(HttpClient Client, Guid Id, string Name)> Account()
    {
        var client = factory.CreateClient();
        var name = "okur_" + Guid.NewGuid().ToString("N")[..16];
        var credentials = new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!", Nickname = name };
        (await client.PostAsJsonAsync("/api/identity/register", credentials)).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync("/api/identity/login", credentials);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.GetProperty("accessToken").GetString());
        return (client, data.GetProperty("userId").GetGuid(), name);
    }
    private async Task<Guid> Novel()
    {
        var book = await CatalogTestData.CreateBookAsync(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Editions.Add(new Edition(book, "Test Roman", "tur", EditionFormat.Paperback, coverImageUrl: "https://example.com/cover.jpg"));
        db.Editions.Add(new Edition(book, "Test Roman ikinci baskı", "tur", EditionFormat.Hardcover, coverImageUrl: "https://example.com/cover2.jpg"));
        await db.SaveChangesAsync();
        return book;
    }
    private static async Task<Guid> Add(HttpClient client, Guid book, string status)
    {
        var response = await client.PostAsJsonAsync("/api/reading/library", new { BookId = book, Status = status });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("libraryEntryId").GetGuid();
    }
    private static async Task Rate(HttpClient client, Guid book, int score)
    {
        (await client.PutAsJsonAsync($"/api/community/books/{book}/rating", new {
            Theme = score, Language = score, Characters = score, Structure = score,
            Pace = score, Impact = score, Experience = score })).EnsureSuccessStatusCode();
    }
    private static async Task Counts(HttpClient client, int reading, int evaluation)
    {
        using var response = await client.GetAsync("/api/readers/me/achievements");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl?.NoStore);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = data.GetProperty("items");
        Assert.Equal(reading, items[0].GetProperty("count").GetInt32());
        Assert.Equal(evaluation, items[1].GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task TracksAreIndependent_AndEditsReaddsRemovalsDoNotInflateCounts()
    {
        var (owner, _, _) = await Account();
        var first = await Novel();
        var second = await Novel();
        var firstEntry = await Add(owner, first, "Read");
        var secondEntry = await Add(owner, second, "WantToRead");
        await Counts(owner, 1, 0);
        await Rate(owner, second, 8);
        await Counts(owner, 1, 1);
        await Rate(owner, second, 9);
        await Counts(owner, 1, 1);
        (await owner.PatchAsJsonAsync($"/api/reading/library/{secondEntry}/status", new { Status = "Read" })).EnsureSuccessStatusCode();
        await Counts(owner, 2, 1);
        (await owner.DeleteAsync($"/api/reading/library/{firstEntry}")).EnsureSuccessStatusCode();
        await Counts(owner, 1, 1);
        await Add(owner, first, "Read");
        await Counts(owner, 2, 1);
        (await owner.PatchAsJsonAsync($"/api/reading/library/{secondEntry}/status", new { Status = "Reading" })).EnsureSuccessStatusCode();
        (await owner.DeleteAsync($"/api/community/books/{second}/rating")).EnsureSuccessStatusCode();
        await Counts(owner, 1, 0);
    }

    [Fact]
    public async Task LegacyRatingsAndIneligibleBooksAreExcluded()
    {
        var (owner, id, _) = await Account();
        var eligible = await Novel();
        var noCover = await CatalogTestData.CreateBookAsync(factory);
        await Add(owner, eligible, "Read");
        await Add(owner, noCover, "Read");
        await Rate(owner, noCover, 8);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
            var ratingId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO community."BookRatings" ("Id", "UserId", "BookId", "LegacyScore", "CreatedAtUtc", "UpdatedAtUtc")
                VALUES ({ratingId}, {id}, {eligible}, 5, {now}, {now})
                """);
        }
        await Counts(owner, 1, 0);
        await Rate(owner, eligible, 8);
        await Counts(owner, 1, 1);
    }

    [Fact]
    public async Task AchievementsRespectPrivacyAndCurrentUserScope()
    {
        var (owner, _, name) = await Account();
        var (other, _, _) = await Account();
        using var anonymous = factory.CreateClient();
        await Add(owner, await Novel(), "Read");
        await Counts(other, 0, 0);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/readers/me/achievements")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/readers/" + name)).StatusCode);
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = name, IsPublic = true })).EnsureSuccessStatusCode();
        var publicProfile = await anonymous.GetFromJsonAsync<JsonElement>("/api/readers/" + name);
        Assert.Equal(1, publicProfile.GetProperty("achievements")[0].GetProperty("count").GetInt32());
        Assert.Equal("İlk Sayfa", publicProfile.GetProperty("achievements")[0].GetProperty("current").GetProperty("title").GetString());
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = name, IsPublic = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/readers/" + name)).StatusCode);
    }
    [Fact]
    public async Task Own_ratings_are_returned_only_for_requested_books_of_the_signed_in_reader()
    {
        var (owner, _, _) = await Account();
        var (other, _, _) = await Account();
        var rated = await Novel();
        var unrated = await Novel();
        await Rate(owner, rated, 8);
        await Rate(other, unrated, 4);

        var data = await owner.GetFromJsonAsync<JsonElement>($"/api/readers/me/ratings?bookIds={rated},{unrated}");
        var items = data.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(rated, items[0].GetProperty("bookId").GetGuid());
        Assert.Equal(8, items[0].GetProperty("overallScore").GetDecimal());

        Assert.Equal(0, (await owner.GetFromJsonAsync<JsonElement>("/api/readers/me/ratings")).GetProperty("items").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/api/readers/me/ratings?bookIds=abc")).StatusCode);
        var tooMany = string.Join(",", Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.GetAsync("/api/readers/me/ratings?bookIds=" + tooMany)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/readers/me/ratings")).StatusCode);
    }
}
