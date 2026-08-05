using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.Modules.Catalog.Application.Authors.Create;
using KitapKivo.Modules.Catalog.Application.Books.Create;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' was not found.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<CreateAuthorHandler>();
        services.AddScoped<CreateBookHandler>();
        return services;
    }
}