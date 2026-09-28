using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KitapKivo.Modules.Identity.Infrastructure.Authentication;
using KitapKivo.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KitapKivo.Api.IntegrationTests.Identity;

public sealed class GoogleLoginTests(KitapKivoWebApplicationFactory factory) : IClassFixture<KitapKivoWebApplicationFactory>
{
    private sealed class Validator(string subject, string email) : IGoogleIdentityValidator
    {
        public Task<GoogleIdentity?> ValidateAsync(string credential, string clientId, string nonce, CancellationToken ct)
            => Task.FromResult<GoogleIdentity?>(credential == "valid" ? new(subject, email) : null);
    }
    [Fact]
    public async Task GoogleCreatesPrivateAccountWithSessionAndRejectsReplayAndEmailLinking()
    {
        var subject = Guid.NewGuid().ToString();
        var email = subject + "@gmail.com";
        using var app = factory.WithWebHostBuilder(builder => {
            builder.UseSetting("Google:ClientId", "test-client");
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IGoogleIdentityValidator>();
                services.AddSingleton<IGoogleIdentityValidator>(new Validator(subject, email));
            });
        });
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/identity/google", new { Credential = "valid" })).StatusCode);
        (await client.PostAsync("/api/identity/google/challenge", null)).EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync("/api/identity/google", new { Credential = "valid" });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(session.GetProperty("needsNickname").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(session.GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/identity/google", new { Credential = "valid" })).StatusCode);
        (await client.PostAsync("/api/identity/refresh", null)).EnsureSuccessStatusCode();
        using var scope = app.Services.CreateScope();
        var user = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.SingleAsync(u => u.Email == email);
        Assert.False(user.IsPublic);
        Assert.Null(user.Nickname);
        Assert.Equal(subject, user.GoogleSubject);
        (await client.PostAsync("/api/identity/google/challenge", null)).EnsureSuccessStatusCode();
        var again = await client.PostAsJsonAsync("/api/identity/google", new { Credential = "valid" });
        Assert.Equal(user.Id, (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid());

        using var collisionApp = factory.WithWebHostBuilder(builder => {
            builder.UseSetting("Google:ClientId", "test-client");
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IGoogleIdentityValidator>();
                services.AddSingleton<IGoogleIdentityValidator>(new Validator(Guid.NewGuid().ToString(), email));
            });
        });
        using var other = collisionApp.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        (await other.PostAsync("/api/identity/google/challenge", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await other.PostAsJsonAsync("/api/identity/google", new { Credential = "valid" })).StatusCode);
    }
    [Fact]
    public async Task DisabledGoogleDoesNotIssueChallenges()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/identity/google/challenge", null)).StatusCode);
    }
}
