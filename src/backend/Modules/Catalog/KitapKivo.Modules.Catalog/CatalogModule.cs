using KitapKivo.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using KitapKivo.Modules.Catalog.Application
    .Authors.Create;
using KitapKivo.Modules.Catalog.Application
    .Books.Browse;
using KitapKivo.Modules.Catalog.Application
    .Books.Create;
using KitapKivo.Modules.Catalog.Application
    .Books.GetById;
using KitapKivo.Modules.Catalog.Application
    .Books.GetExternalDetails;
using KitapKivo.Modules.Catalog.Application
    .Books.ImportExternal;
using KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;
using KitapKivo.Modules.Catalog.Application
    .Books.Search;
using KitapKivo.Modules.Catalog.Application
    .Categories.GetAll;
using KitapKivo.Modules.Catalog.Application
    .Imports.Create;
using KitapKivo.Modules.Catalog.Application
    .Imports.GetById;
using KitapKivo.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;
using KitapKivo.Modules.Catalog.Infrastructure
    .Imports;
using KitapKivo.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KitapKivo.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString(
                "Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' " +
                "was not found.");

        services.AddDbContext<CatalogDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString));

        services.Configure<OpenLibraryOptions>(
            configuration.GetSection(
                OpenLibraryOptions.SectionName));

        services.AddOptions<
                CatalogImportJobWorkerOptions>()
            .Bind(
                configuration.GetSection(
                    CatalogImportJobWorkerOptions
                        .SectionName))
            .Validate(
                options =>
                    options.PollIntervalSeconds > 0,
                "Poll interval must be greater than zero.")
            .Validate(
                options =>
                    options.DelayBetweenPagesSeconds >= 0,
                "Page delay cannot be negative.")
            .Validate(
                options =>
                    options.MaximumRetryCount > 0,
                "Maximum retry count must be greater than zero.")
            .Validate(
                options =>
                    options.InitialRetryDelaySeconds > 0,
                "Initial retry delay must be greater than zero.")
            .Validate(
                options =>
                    options.MaximumRetryDelaySeconds >=
                    options.InitialRetryDelaySeconds,
                "Maximum retry delay must be greater than " +
                "or equal to the initial retry delay.")
            .Validate(
                options =>
                    options.InterruptedJobTimeoutMinutes > 0,
                "Interrupted job timeout must be greater than zero.")
            .ValidateOnStart();

        services.AddHttpClient<
            IExternalBookSearchClient,
            OpenLibrarySearchClient>(
            ConfigureOpenLibraryClient);

        services.AddHttpClient<
            IExternalBookDetailsClient,
            OpenLibraryBookDetailsClient>(
            ConfigureOpenLibraryClient);

        services.AddHttpClient<
            IExternalBookEditionClient,
            OpenLibraryBookEditionClient>(
            ConfigureOpenLibraryClient);

        services.AddScoped<CreateAuthorHandler>();
        services.AddScoped<CreateBookHandler>();
        services.AddScoped<GetBookByIdHandler>();
        services.AddScoped<SearchBooksHandler>();

        services.AddScoped<
            GetExternalBookDetailsHandler>();

        services.AddScoped<
            ResolveExternalBookHandler>();

        services.AddScoped<
            ImportExternalBooksHandler>();

        services.AddScoped<
            CreateCatalogImportJobHandler>();

        services.AddScoped<
            GetCatalogImportJobByIdHandler>();

        services.AddScoped<
            CatalogImportJobProcessor>();

        services.AddScoped<BrowseBooksHandler>();
        services.AddScoped<GetCategoriesHandler>();

        bool workerEnabled =
            configuration.GetValue(
                $"{CatalogImportJobWorkerOptions.SectionName}:" +
                "WorkerEnabled",
                true);

        if (workerEnabled)
        {
            services.AddHostedService<
                CatalogImportJobWorker>();
        }

        return services;
    }

    private static void ConfigureOpenLibraryClient(
        IServiceProvider serviceProvider,
        HttpClient client)
    {
        OpenLibraryOptions options =
            serviceProvider
                .GetRequiredService<
                    IOptions<OpenLibraryOptions>>()
                .Value;

        client.BaseAddress = new Uri(
            $"{options.BaseUrl.TrimEnd('/')}/",
            UriKind.Absolute);

        client.Timeout = TimeSpan.FromSeconds(
            options.TimeoutSeconds);

        client.DefaultRequestHeaders
            .UserAgent
            .ParseAdd(options.UserAgent);
    }
}