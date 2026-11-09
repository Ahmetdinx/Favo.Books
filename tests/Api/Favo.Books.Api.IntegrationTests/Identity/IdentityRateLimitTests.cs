using System.Net;
using System.Net.Http.Json;

using Favo.Books.Modules.Identity.Application.Abstractions.Security;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Api.IntegrationTests.Identity;

public sealed class IdentityRateLimitTests(FavoBooksWebApplicationFactory factory)
    : IClassFixture<FavoBooksWebApplicationFactory>
{
    private const int LoginPermits = 10;
    private const string ConnectionHeader = "X-Test-Connection-Ip";

    [Fact]
    public async Task Login_OverLimit_Returns429ForThatClientOnly()
    {
        HttpClient client = CreateClient();

        for (int i = 0; i < LoginPermits; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "203.0.113.1")).StatusCode);
        }

        HttpResponseMessage rejected = await Login(client, "203.0.113.1");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "203.0.113.2")).StatusCode);
    }

    [Fact]
    public async Task Login_BehindLoopbackProxy_LimitsForwardedClient()
    {
        HttpClient client = CreateClient();

        for (int i = 0; i < LoginPermits; i++)
        {
            await Login(client, "127.0.0.1", forwardedFor: "203.0.113.10");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, "127.0.0.1", "203.0.113.10")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Login(client, "127.0.0.1", "203.0.113.11")).StatusCode);
    }

    [Fact]
    public async Task Login_FromUntrustedAddress_IgnoresSpoofedForwardedHeader()
    {
        HttpClient client = CreateClient();

        for (int i = 0; i < LoginPermits; i++)
        {
            await Login(client, "203.0.113.20", forwardedFor: $"198.51.100.{i}");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await Login(client, "203.0.113.20", "198.51.100.200")).StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_StillVerifiesPassword()
    {
        CountingPasswordHasher hasher = new();
        HttpClient client = factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(
                services => services.AddSingleton<IPasswordHasher>(hasher)))
            .CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/identity/login",
            new { Email = $"{Guid.NewGuid():N}@example.test", Password = "Wrong-Password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, hasher.VerifyCalls);
    }

    private HttpClient CreateClient() => factory
        .WithWebHostBuilder(builder => builder.ConfigureServices(
            services => services.AddSingleton<IStartupFilter>(new ConnectionAddressFilter())))
        .CreateClient();

    private static Task<HttpResponseMessage> Login(HttpClient client, string connection, string? forwardedFor = null)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/api/identity/login")
        {
            Content = JsonContent.Create(new { Email = $"{Guid.NewGuid():N}@example.test", Password = "Wrong-Password-1" })
        };
        request.Headers.Add(ConnectionHeader, connection);
        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        return client.SendAsync(request);
    }

    private sealed class ConnectionAddressFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(context.Request.Headers[ConnectionHeader].ToString());
                return nextMiddleware();
            });
            next(app);
        };
    }

    private sealed class CountingPasswordHasher : IPasswordHasher
    {
        public int VerifyCalls { get; private set; }

        public string Hash(string password) => "hash";

        public bool Verify(string passwordHash, string providedPassword)
        {
            VerifyCalls++;
            return false;
        }
    }
}
