using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.Modules.Catalog.Application.Authors.Create;
using KitapKivo.Modules.Catalog.Application.Books.Create;
using KitapKivo.Modules.Catalog.Application.Books.GetById;
using KitapKivo.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary;

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

        services.Configure<OpenLibraryOptions>(
            configuration.GetSection(OpenLibraryOptions.SectionName));

        services.AddScoped<CreateAuthorHandler>();
        services.AddScoped<CreateBookHandler>();
        services.AddScoped<GetBookByIdHandler>();
        return services;
    }
}