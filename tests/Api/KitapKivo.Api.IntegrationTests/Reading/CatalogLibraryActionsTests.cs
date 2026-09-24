using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KitapKivo.Api.IntegrationTests.Reading;

public sealed class CatalogLibraryActionsTests(KitapKivoWebApplicationFactory factory) : IClassFixture<KitapKivoWebApplicationFactory>
{
    private async Task<HttpClient> Account()
    {
        var client = factory.CreateClient();
        var credentials = new { Email = Guid.NewGuid() + "@example.com", Password = "Test1234!",
            Nickname = "okur_" + Guid.NewGuid().ToString("N")[..16] };
        (await client.PostAsJsonAsync("/api/identity/register", credentials)).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync("/api/identity/login", credentials);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }
    private static async Task<JsonElement> Add(HttpClient client, Guid book, string status = "Read")
    {
        var response = await client.PostAsJsonAsync("/api/reading/library", new { BookId = book, Status = status });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Lookup_IsBounded_Authenticated_AndScopedToCurrentUser()
    {
        using var owner = await Account();
        using var other = await Account();
        using var visitor = factory.CreateClient();
        var book = await CatalogTestData.CreateBookAsync(factory);
        await Add(owner, book);
        var request = new { BookIds = new[] { book, book, Guid.NewGuid() } };
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PostAsJsonAsync("/api/reading/library/lookup", request)).StatusCode);
        var response = await owner.PostAsJsonAsync("/api/reading/library/lookup", request);
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl?.NoStore);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(data.GetProperty("items").EnumerateArray());
        Assert.Equal("Read", data.GetProperty("items")[0].GetProperty("status").GetString());
        var others = await other.PostAsJsonAsync("/api/reading/library/lookup", request);
        Assert.Empty((await others.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").EnumerateArray());
        foreach (var ids in new[] { new[] { Guid.Empty }, Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToArray() })
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync("/api/reading/library/lookup", new { BookIds = ids })).StatusCode);
    }

    [Fact]
    public async Task UndoAddition_RemovesOnlyTheCallersUnchangedEntry()
    {
        using var owner = await Account();
        using var other = await Account();
        var book = await CatalogTestData.CreateBookAsync(factory);
        var added = await Add(owner, book);
        var id = added.GetProperty("libraryEntryId").GetGuid();
        var request = new { ExpectedUpdatedAtUtc = added.GetProperty("updatedAtUtc").GetDateTimeOffset(), PreviousStatus = (string?)null };
        Assert.Equal(HttpStatusCode.Conflict, (await other.PostAsJsonAsync($"/api/reading/library/{id}/undo", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/reading/library/books/{book}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/reading/library/{id}/undo", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/reading/library/books/{book}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/reading/library/{id}/undo", request)).StatusCode);
    }

    [Fact]
    public async Task UndoStatus_RestoresPreviousState_AndRejectsStaleUndo()
    {
        using var owner = await Account();
        var book = await CatalogTestData.CreateBookAsync(factory);
        var added = await Add(owner, book, "OnHold");
        var id = added.GetProperty("libraryEntryId").GetGuid();
        var update = await owner.PatchAsJsonAsync($"/api/reading/library/{id}/status", new { Status = "Read" });
        update.EnsureSuccessStatusCode();
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        var request = new { ExpectedUpdatedAtUtc = updated.GetProperty("updatedAtUtc").GetDateTimeOffset(), PreviousStatus = "OnHold" };
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PostAsJsonAsync($"/api/reading/library/{id}/undo", request)).StatusCode);
        var entry = await owner.GetFromJsonAsync<JsonElement>($"/api/reading/library/books/{book}");
        Assert.Equal("OnHold", entry.GetProperty("status").GetString());
        (await owner.PatchAsJsonAsync($"/api/reading/library/{id}/status", new { Status = "Reading" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync($"/api/reading/library/{id}/undo", request)).StatusCode);
        var latest = await owner.GetFromJsonAsync<JsonElement>($"/api/reading/library/books/{book}");
        Assert.Equal("Reading", latest.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync($"/api/reading/library/{id}/undo",
            new { ExpectedUpdatedAtUtc = latest.GetProperty("updatedAtUtc").GetDateTimeOffset(), PreviousStatus = "invalid" })).StatusCode);
    }

    [Fact]
    public async Task ConcurrentAdditions_CreateOnlyOneEntry()
    {
        using var owner = await Account();
        var book = await CatalogTestData.CreateBookAsync(factory);
        var responses = await Task.WhenAll(
            owner.PostAsJsonAsync("/api/reading/library", new { BookId = book, Status = "Read" }),
            owner.PostAsJsonAsync("/api/reading/library", new { BookId = book, Status = "Read" }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
    }
}
