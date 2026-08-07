using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.Modules.Identity.Persistence;
using KitapKivo.Modules.Reading.Persistence;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
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
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}