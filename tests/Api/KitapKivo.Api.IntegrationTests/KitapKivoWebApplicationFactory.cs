using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.Modules.Identity.Persistence;
using KitapKivo.Modules.Reading.Persistence;
using KitapKivo.Modules.Profiles.Persistence;
using KitapKivo.Modules.Community.Persistence;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.PostgreSql;

namespace KitapKivo.Api.IntegrationTests;

public sealed class KitapKivoWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string JwtSecret =
        "integration-tests-only-secret-key-at-least-32-characters";

    private readonly PostgreSqlContainer _postgresContainer =
        new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("kitapkivo_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:Database",
            _postgresContainer.GetConnectionString());

        builder.UseSetting(
            "Jwt:Issuer",
            "KitapKivo.Api.IntegrationTests");

        builder.UseSetting(
            "Jwt:Audience",
            "KitapKivo.TestClient");

        builder.UseSetting(
            "Jwt:Secret",
            JwtSecret);

        builder.UseSetting(
            "Jwt:ExpirationMinutes",
            "15");

        builder.UseSetting(
            "Catalog:ImportJobs:WorkerEnabled",
            "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<
                IExternalBookEditionClient>();

            services.AddSingleton<
                IExternalBookEditionClient,
                StubExternalBookEditionClient>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        using IServiceScope scope =
            Services.CreateScope();

        IdentityDbContext identityDbContext =
            scope.ServiceProvider.GetRequiredService<
                IdentityDbContext>();

        await identityDbContext.Database.MigrateAsync();

        CatalogDbContext catalogDbContext =
            scope.ServiceProvider.GetRequiredService<
                CatalogDbContext>();

        await catalogDbContext.Database.MigrateAsync();

        ReadingDbContext readingDbContext =
            scope.ServiceProvider.GetRequiredService<
                ReadingDbContext>();

        await readingDbContext.Database.MigrateAsync();

        ProfilesDbContext profilesDbContext =
    scope.ServiceProvider.GetRequiredService<
        ProfilesDbContext>();

        await profilesDbContext.Database
            .MigrateAsync();

        CommunityDbContext communityDbContext =
            scope.ServiceProvider.GetRequiredService<CommunityDbContext>();
        await communityDbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private sealed class StubExternalBookEditionClient
        : IExternalBookEditionClient
    {
        public Task<ExternalBookEditionDetails?>
            GetBestForWorkAsync(
                string externalWorkId,
                CancellationToken cancellationToken = default)
        {
            string editionExternalId =
                externalWorkId.Replace(
                    "/works/",
                    "/books/",
                    StringComparison.OrdinalIgnoreCase);

            if (editionExternalId.EndsWith(
                    "W",
                    StringComparison.OrdinalIgnoreCase))
            {
                editionExternalId =
                    $"{editionExternalId[..^1]}M";
            }

            var edition =
                new ExternalBookEditionDetails(
                    "OpenLibrary",
                    editionExternalId,
                    "Türkçe Test Baskısı",
                    "tur",
                    "Paperback",
                    null,
                    null,
                    320,
                    2024,
                    null,
                    null);

            return Task.FromResult<
                ExternalBookEditionDetails?>(edition);
        }
    }
}
