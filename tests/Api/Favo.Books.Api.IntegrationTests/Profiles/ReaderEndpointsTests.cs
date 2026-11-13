using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Profiles;

public sealed class ReaderEndpointsTests(FavoBooksWebApplicationFactory factory) : IClassFixture<FavoBooksWebApplicationFactory>
{
    private static string Name() => "okur_" + Guid.NewGuid().ToString("N")[..16];
    private async Task<(HttpClient Client, string Nickname, Guid Id)> Account(string? nickname = null)
    {
        var client = factory.CreateClient();
        nickname ??= Name();
        var credentials = new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!", Nickname = nickname };
        using var register = await client.PostAsJsonAsync("/api/identity/register", credentials);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/identity/login", credentials);
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return (client, nickname, body.GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task Visibility_Search_DirectLink_AndPrivateNotes_AreEnforced()
    {
        var (owner, nickname, _) = await Account();
        using var visitor = factory.CreateClient();
        using var settings = await owner.GetAsync("/api/readers/me");
        Assert.False((await settings.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isPublic").GetBoolean());
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/readers/" + nickname)).StatusCode);

        var book = await CatalogTestData.CreateBookAsync(factory);
        (await owner.PostAsJsonAsync("/api/reading/library", new { BookId = book, Status = "Read" })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync("/api/profiles/me/bedside-book", new { BookId = book, PersonalNote = "secret bedside note" })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync("/api/profiles/me/influential-books/1", new { BookId = book, PersonalReflection = "secret reflection" })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync($"/api/community/books/{book}/rating", new { Theme = 8, Language = 9, Characters = 7, Structure = 8, Pace = 6, Impact = 9, Experience = 8 })).EnsureSuccessStatusCode();
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = true })).EnsureSuccessStatusCode();

        using var response = await visitor.GetAsync("/api/readers/" + nickname.ToUpperInvariant());
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl?.NoStore);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret", text);
        Assert.DoesNotContain("email", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("personalNote", text, StringComparison.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(text);
        Assert.Equal(book, document.RootElement.GetProperty("showcase").GetProperty("bedsideBookId").GetGuid());
        Assert.Equal(1, document.RootElement.GetProperty("library").GetProperty("totalCount").GetInt32());
        Assert.Equal(6, document.RootElement.GetProperty("ratings")[0].GetProperty("pace").GetInt32());
        Assert.Contains(nickname, await visitor.GetStringAsync("/api/readers/?query=" + nickname));

        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/readers/" + nickname)).StatusCode);
        Assert.DoesNotContain(nickname, await visitor.GetStringAsync("/api/readers/?query=" + nickname));
        var privateNote = await owner.GetStringAsync("/api/profiles/me/bedside-book");
        Assert.Contains("secret bedside note", privateNote);
    }

    [Fact]
    public async Task Nicknames_AreRequired_Normalized_Unique_AndRenamable()
    {
        using var visitor = factory.CreateClient();
        foreach (var nickname in new string?[] { null, "", "ab", "1abc", "space name", "abc@", "ADMIN", "kitapkivo", "Favo", new string('a', 25) })
        {
            var response = await visitor.PostAsJsonAsync("/api/identity/register",
                new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!", Nickname = nickname });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        var name = Name();
        var (owner, _, _) = await Account("  " + name.ToUpperInvariant() + "  ");
        var duplicate = await visitor.PostAsJsonAsync("/api/identity/register",
            new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!", Nickname = name });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var (other, otherName, _) = await Account();
        Assert.Equal(HttpStatusCode.Conflict, (await other.PutAsJsonAsync("/api/readers/me", new { Nickname = name, IsPublic = true })).StatusCode);
        var otherSettings = await other.GetFromJsonAsync<JsonElement>("/api/readers/me");
        Assert.Equal(otherName, otherSettings.GetProperty("nickname").GetString());
        Assert.False(otherSettings.GetProperty("isPublic").GetBoolean());

        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = name, IsPublic = true })).EnsureSuccessStatusCode();
        var renamed = Name();
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = renamed, IsPublic = true })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/readers/" + name)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync("/api/readers/" + renamed)).StatusCode);
    }

    [Fact]
    public async Task Settings_RequireAuthentication_AndOnlyChangeTheCaller()
    {
        using var visitor = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/readers/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PutAsJsonAsync("/api/readers/me", new { Nickname = Name(), IsPublic = true })).StatusCode);
        var (first, name, _) = await Account();
        var (_, _, otherId) = await Account();
        (await first.PutAsJsonAsync("/api/readers/me", new { Nickname = name, IsPublic = true, UserId = otherId })).EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        Assert.False((await db.Users.SingleAsync(u => u.Id == otherId)).IsPublic);
    }

    [Fact]
    public async Task ExistingAccounts_CanChooseNickname_WithoutChangingIdentity()
    {
        var (owner, _, id) = await Account();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Users.Where(u => u.Id == id).ExecuteUpdateAsync(s => s.SetProperty(u => u.Nickname, (string?)null));
        }
        var before = await owner.GetFromJsonAsync<JsonElement>("/api/readers/me");
        Assert.Equal(JsonValueKind.Null, before.GetProperty("nickname").ValueKind);
        var nickname = Name();
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = false })).EnsureSuccessStatusCode();
        using var verifyScope = factory.Services.CreateScope();
        Assert.Equal(id, (await verifyScope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.SingleAsync(u => u.Nickname == nickname)).Id);
    }

    [Fact]
    public async Task ConcurrentClaims_HaveExactlyOneWinner()
    {
        var (a, _, _) = await Account();
        var (b, _, _) = await Account();
        var nickname = Name();
        var results = await Task.WhenAll(
            a.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = false }),
            b.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = false }));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AvatarSelection_Persists_IsPublicOnlyWithProfile_AndOmittedValueIsPreserved()
    {
        var (owner, nickname, _) = await Account();
        using var visitor = factory.CreateClient();
        var initial = await owner.GetFromJsonAsync<JsonElement>("/api/readers/me");
        Assert.Equal("owl", initial.GetProperty("avatarId").GetString());
        foreach (var avatar in new[] { "owl", "quill", "lantern", "moon", "compass", "key", "hourglass", "rose", "book", "tree", "anchor", "sun" })
        {
            var response = await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = false, AvatarId = avatar });
            response.EnsureSuccessStatusCode();
            Assert.Equal(avatar, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("avatarId").GetString());
        }
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = true })).EnsureSuccessStatusCode();
        var publicProfile = await visitor.GetFromJsonAsync<JsonElement>("/api/readers/" + nickname);
        Assert.Equal("sun", publicProfile.GetProperty("avatarId").GetString());
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = false })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/readers/" + nickname)).StatusCode);
    }

    [Fact]
    public async Task InvalidAvatar_DoesNotChangeNicknameOrVisibility_AndCannotGrantBadges()
    {
        var (owner, nickname, _) = await Account();
        foreach (var invalid in new[] { "", "not-an-avatar", "https://example.com/a.png", new string('x', 100) })
        {
            var response = await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = Name(), IsPublic = true, AvatarId = invalid });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        var current = await owner.GetFromJsonAsync<JsonElement>("/api/readers/me");
        Assert.Equal(nickname, current.GetProperty("nickname").GetString());
        Assert.False(current.GetProperty("isPublic").GetBoolean());
        Assert.Equal("owl", current.GetProperty("avatarId").GetString());
        (await owner.PutAsJsonAsync("/api/readers/me", new { Nickname = nickname, IsPublic = true, AvatarId = "lantern", ReadingLevel = 1000, EvaluationLevel = 1000 })).EnsureSuccessStatusCode();
        var progress = await owner.GetFromJsonAsync<JsonElement>("/api/readers/me/achievements");
        Assert.All(progress.GetProperty("items").EnumerateArray(), item => Assert.Equal(JsonValueKind.Null, item.GetProperty("current").ValueKind));
    }

    [Fact]
    public async Task Search_IsBounded_Paginated_AndDoesNotTreatUnderscoreAsWildcard()
    {
        var prefix = Name()[..12];
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            for (var i = 0; i < 22; i++)
            {
                var user = User.Create(Guid.NewGuid() + "@example.com", "unused-test-hash", DateTimeOffset.UtcNow);
                user.SetProfile(prefix + "_" + i.ToString("D2"), i != 21);
                db.Users.Add(user);
            }
            var excluded = User.Create(Guid.NewGuid() + "@example.com", "unused-test-hash", DateTimeOffset.UtcNow);
            excluded.SetProfile(prefix + "xother", true);
            db.Users.Add(excluded);
            await db.SaveChangesAsync();
        }
        using var visitor = factory.CreateClient();
        using var first = await visitor.GetAsync("/api/readers/?query=" + prefix + "_");
        Assert.True(first.Headers.CacheControl?.NoStore);
        var page1 = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(20, page1.GetProperty("items").GetArrayLength());
        Assert.True(page1.GetProperty("hasMore").GetBoolean());
        var page2 = await visitor.GetFromJsonAsync<JsonElement>("/api/readers/?page=2&query=" + prefix + "_");
        Assert.Single(page2.GetProperty("items").EnumerateArray());
        Assert.Equal(prefix + "_20", page2.GetProperty("items")[0].GetString());
        Assert.False(page2.GetProperty("hasMore").GetBoolean());
        foreach (var url in new[] { "/api/readers/?query=a", "/api/readers/?query=ab&page=0", "/api/readers/?query=%25", "/api/readers/example?page=-1" })
            Assert.Equal(HttpStatusCode.BadRequest, (await visitor.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Referrals_count_sign_ups_through_a_readers_share_and_name_only_public_ones()
    {
        var (sharer, sharerName, _) = await Account();
        async Task<HttpClient> Invited(string? referrer, string? source)
        {
            var client = factory.CreateClient();
            var name = Name();
            var credentials = new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!", Nickname = name, Referrer = referrer, ReferralSource = source };
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/identity/register", credentials)).StatusCode);
            var login = await (await client.PostAsJsonAsync("/api/identity/login", credentials)).Content.ReadFromJsonAsync<JsonElement>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
            await client.PutAsJsonAsync("/api/readers/me", new { Nickname = name, IsPublic = source == "kart" });
            return client;
        }

        await Invited(sharerName.ToUpperInvariant(), "kart");
        await Invited(sharerName, "bilinmeyen-kaynak");
        await Invited("boyle_biri_yok_" + Guid.NewGuid().ToString("N")[..6], "link");
        await Invited("geçersiz ad!", "link");

        var summary = await sharer.GetFromJsonAsync<JsonElement>("/api/readers/me/referrals");
        Assert.Equal(2, summary.GetProperty("total").GetInt32());
        Assert.Equal(1, summary.GetProperty("readers").GetArrayLength());
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/readers/me/referrals")).StatusCode);
    }
}
