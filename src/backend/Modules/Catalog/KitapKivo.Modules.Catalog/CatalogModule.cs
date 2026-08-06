using KitapKivo.Modules.Catalog.Persistence;
using KitapKivo.Modules.Catalog.Application.Authors.Create;
using KitapKivo.Modules.Catalog.Application.Books.Create;
using KitapKivo.Modules.Catalog.Application.Books.GetById;
using KitapKivo.Modules.Catalog.Infrastructure.ExternalBooks.OpenLibrary;
using KitapKivo.Modules.Catalog.Application.Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application.Books.Search;
using KitapKivo.Modules.Catalog.Application.Books.GetExternalDetails;

using Microsoft.Extensions.Options;
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

        services.AddHttpClient<
          IExternalBookSearchClient,
          OpenLibrarySearchClient>(
          ConfigureOpenLibraryClient);

        services.AddHttpClient<
            IExternalBookDetailsClient,
            OpenLibraryBookDetailsClient>(
            ConfigureOpenLibraryClient);

        services.AddScoped<CreateAuthorHandler>();
        services.AddScoped<CreateBookHandler>();
        services.AddScoped<GetBookByIdHandler>();
        services.AddScoped<SearchBooksHandler>();
        services.AddScoped<GetExternalBookDetailsHandler>();
        return services;
    }

    private static void ConfigureOpenLibraryClient(
     IServiceProvider serviceProvider,
     HttpClient client)
    {
        OpenLibraryOptions options = serviceProvider
            .GetRequiredService<IOptions<OpenLibraryOptions>>()
            .Value;

        client.BaseAddress = new Uri(
            $"{options.BaseUrl.TrimEnd('/')}/",
            UriKind.Absolute);

        client.Timeout = TimeSpan.FromSeconds(
            options.TimeoutSeconds);

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            options.UserAgent);
    }
}