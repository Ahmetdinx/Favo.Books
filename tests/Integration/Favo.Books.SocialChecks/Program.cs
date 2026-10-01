extern alias books;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Favo.Books.Modules.Identity.Persistence;
using Favo.Books.Modules.Catalog.Persistence;
using Favo.Books.Modules.Reading.Persistence;
using Favo.Books.Modules.Profiles.Persistence;
using Favo.Books.Modules.Community.Persistence;

var root = Environment.GetEnvironmentVariable("FAVO_BOOKS_ROOT") ?? Directory.GetCurrentDirectory();
await using var database = new PostgreSqlBuilder("postgres:18.4-alpine3.24")
    .WithDatabase("favo_social_checks").WithUsername("postgres").WithPassword("favo-tests-only").Build();
await database.StartAsync();
await using var factory = new SocialFactory(root, database.GetConnectionString());
if (args.Contains("--serve")) factory.UseKestrel(5118);
using (var scope = factory.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ReadingDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<ProfilesDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<CommunityDbContext>().Database.MigrateAsync();
}
using var anonymous = factory.CreateClient();
var checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); checks++; }
async Task<JsonElement> Get(HttpClient client, string path) => await client.GetFromJsonAsync<JsonElement>(path);
async Task<HttpClient> Reader(string name)
{
    var client = factory.CreateClient();
    (await client.PostAsJsonAsync("/api/identity/register", new { email = name + "@example.test", nickname = name, password = "Social-Test-Only-2026!" })).EnsureSuccessStatusCode();
    var response = await client.PostAsJsonAsync("/api/identity/login", new { email = name + "@example.test", password = "Social-Test-Only-2026!" });
    response.EnsureSuccessStatusCode();
    var session = await response.Content.ReadFromJsonAsync<JsonElement>();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
    return client;
}
async Task SetPublic(HttpClient client, string name, bool visible) =>
    (await client.PutAsJsonAsync("/api/readers/me", new { nickname = name, isPublic = visible, avatarId = "owl" })).EnsureSuccessStatusCode();
using var a = await Reader("ada"); using var b = await Reader("deniz"); using var c = await Reader("ece"); using var outsider = await Reader("outsider");
await SetPublic(b, "deniz", true); await SetPublic(c, "ece", true); await SetPublic(outsider, "outsider", true);
Check((await anonymous.GetAsync("/api/readers/me/activity")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous activity denied");
Check((await anonymous.PutAsync("/api/readers/deniz/follow", null)).StatusCode == HttpStatusCode.Unauthorized, "Anonymous follow denied");
Check((await a.PutAsync("/api/readers/deniz/follow", null)).StatusCode == HttpStatusCode.Conflict, "Private actor must opt in before following");
await SetPublic(a, "ada", true);
Check((await a.PutAsync("/api/readers/ada/follow", null)).StatusCode == HttpStatusCode.BadRequest, "Self follow denied");
Check((await a.PutAsync("/api/readers/unknown/follow", null)).StatusCode == HttpStatusCode.NotFound, "Unknown target denied");
var concurrent = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => a.PutAsync("/api/readers/deniz/follow", null)));
Check(concurrent.All(r => r.StatusCode == HttpStatusCode.NoContent), "Concurrent follow is idempotent");
Check((await Get(a, "/api/readers/me/network")).GetProperty("followingCount").GetInt32() == 1, "No duplicate relationships");
Check((await Get(b, "/api/readers/me/connections?kind=followers")).GetProperty("items")[0].GetProperty("nickname").GetString() == "ada", "Follower list belongs to authenticated reader");
(await c.PutAsync("/api/readers/ada/follow", null)).EnsureSuccessStatusCode();
Check((await Get(a, "/api/readers/deniz/network")).GetProperty("isFollowing").GetBoolean(), "Public profile follow state");
Check(!(await Get(b, "/api/readers/ada/network")).GetProperty("isFollowing").GetBoolean(), "Follow is directional");
Check((await a.GetAsync("/api/readers/me/connections?kind=followers&page=0")).StatusCode == HttpStatusCode.BadRequest, "Invalid page rejected");
Check((await a.GetAsync("/api/readers/me/connections?kind=invalid")).StatusCode == HttpStatusCode.BadRequest, "Invalid list kind rejected");
var book = Guid.NewGuid();
using (var scope = factory.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var ids = await db.Users.ToDictionaryAsync(u => u.Nickname!, u => u.Id);
    var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await catalog.Database.ExecuteSqlInterpolatedAsync($"""
        INSERT INTO catalog."Books" ("Id", "Title", "OriginalLanguageCode", "CreatedAtUtc")
        VALUES ({book}, 'Bir Okurun Yolculuğu', 'tr', {DateTimeOffset.UtcNow})
        """);
    foreach (var name in new[] { "deniz", "ece", "outsider" })
    {
        var id = ids[name]; var now = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO reading."LibraryEntries" ("Id", "UserId", "BookId", "Status", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES ({Guid.NewGuid()}, {id}, {book}, 'Reading', {now}, {now})
            """);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO community."BookRatings" ("Id", "UserId", "BookId", "Theme", "Language", "Characters", "Structure", "Pace", "Impact", "Experience", "OverallScore", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES ({Guid.NewGuid()}, {id}, {book}, 9, 9, 9, 9, 9, 9, 9, 9, {now}, {now})
            """);
    }
}
var feed = await Get(a, "/api/readers/me/activity");
Check(feed.GetProperty("reading").GetArrayLength() == 1 && feed.GetProperty("reading")[0].GetProperty("nickname").GetString() == "deniz", "Left feed uses following, excludes outsiders and followers");
Check(feed.GetProperty("reviews").GetArrayLength() == 1 && feed.GetProperty("reviews")[0].GetProperty("nickname").GetString() == "ece", "Right feed uses followers, excludes following and outsiders");
Check(feed.GetProperty("reviews")[0].GetProperty("overallScore").GetDecimal() == 9, "Actual rating score returned");
Check(!feed.ToString().Contains("example.test") && !feed.ToString().Contains("userId"), "Feed contains no private identity fields");
Check((await a.GetAsync("/api/readers/me/activity")).Headers.CacheControl?.NoStore == true, "Activity responses cannot be cached");
await SetPublic(b, "deniz", false); await SetPublic(c, "ece", false);
feed = await Get(a, "/api/readers/me/activity");
Check(feed.GetProperty("reading").GetArrayLength() == 0 && feed.GetProperty("reviews").GetArrayLength() == 0, "Private profiles disappear from both feeds");
Check((await Get(a, "/api/readers/me/connections?kind=followers")).GetProperty("items").GetArrayLength() == 0, "Private followers hidden from lists");
Check((await a.PutAsync("/api/readers/deniz/follow", null)).StatusCode == HttpStatusCode.NotFound, "Private target cannot be followed");
Check((await a.DeleteAsync("/api/readers/deniz/follow")).StatusCode == HttpStatusCode.NoContent, "Existing hidden target can be unfollowed");
Check((await a.DeleteAsync("/api/readers/deniz/follow")).StatusCode == HttpStatusCode.NoContent, "Unfollow is idempotent");
await SetPublic(b, "deniz", true); await SetPublic(c, "ece", true);
Check(!(await Get(a, "/api/readers/deniz/network")).GetProperty("isFollowing").GetBoolean(), "Unfollow persists");
(await a.PutAsync("/api/readers/deniz/follow", null)).EnsureSuccessStatusCode();
// Add readers directly in the disposable database to exercise list pagination without slow password hashing.
using (var scope = factory.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var ada = await db.Users.SingleAsync(u => u.Nickname == "ada");
    for (var i = 0; i < 21; i++) {
        var reader = Favo.Books.Modules.Identity.Domain.Users.User.Create($"page{i}@example.test", "unused-test-only", DateTimeOffset.UtcNow);
        reader.SetProfile($"page{i:00}", true); db.Users.Add(reader);
    }
    await db.SaveChangesAsync();
    var network = scope.ServiceProvider.GetRequiredService<Favo.Books.Modules.Identity.Application.Users.ReaderNetwork>();
    for (var i = 0; i < 21; i++) await network.Follow(ada.Id, $"page{i:00}", default);
}
var first = await Get(a, "/api/readers/me/connections?kind=following&page=1");
var second = await Get(a, "/api/readers/me/connections?kind=following&page=2");
Check(first.GetProperty("items").GetArrayLength() == 20 && first.GetProperty("hasMore").GetBoolean(), "First page bounded to 20");
Check(second.GetProperty("items").GetArrayLength() == 2 && !second.GetProperty("hasMore").GetBoolean(), "Second page has remaining readers");
Check(!first.GetProperty("items").EnumerateArray().Select(r => r.GetProperty("nickname").GetString()).Intersect(second.GetProperty("items").EnumerateArray().Select(r => r.GetProperty("nickname").GetString())).Any(), "Pages do not overlap");
Console.WriteLine($"PASS: {checks} social integration checks; disposable database only.");
if (args.Contains("--serve")) {
    Console.WriteLine("PREVIEW READY http://localhost:5118 — ada@example.test / Social-Test-Only-2026!");
    using var stop = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
    try { await Task.Delay(Timeout.Infinite, stop.Token); } catch (OperationCanceledException) { }
}

sealed class SocialFactory(string root, string connection) : WebApplicationFactory<books::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Path.Combine(root, "src/backend/Api/Favo.Books.Api"));
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", connection);
        builder.UseSetting("Jwt:Secret", "favo-social-test-only-signing-secret-at-least-32-characters");
        builder.UseSetting("Jwt:Issuer", "Favo.SocialTests"); builder.UseSetting("Jwt:Audience", "Favo.SocialTests");
        builder.UseSetting("Jwt:ExpirationMinutes", "60"); builder.UseSetting("Google:ClientId", "");
        builder.UseSetting("Catalog:ImportJobs:WorkerEnabled", "false"); builder.UseSetting("Logging:LogLevel:Default", "Warning");
    }
}
