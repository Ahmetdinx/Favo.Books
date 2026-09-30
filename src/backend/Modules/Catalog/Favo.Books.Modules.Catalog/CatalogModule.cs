using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application
    .Authors.Create;
using Favo.Books.Modules.Catalog.Application
    .Books.Browse;
using Favo.Books.Modules.Catalog.Application
    .Books.Create;
using Favo.Books.Modules.Catalog.Application
    .Books.GetById;
using Favo.Books.Modules.Catalog.Application
    .Books.GetExternalDetails;
using Favo.Books.Modules.Catalog.Application
    .Books.GetSummaries;
using Favo.Books.Modules.Catalog.Application
    .Books.ImportExternal;
using Favo.Books.Modules.Catalog.Application
    .Books.ResolveExternal;
using Favo.Books.Modules.Catalog.Application
    .Books.Search;
using Favo.Books.Modules.Catalog.Application
    .Categories.GetAll;
using Favo.Books.Modules.Catalog.Application
    .Imports.Create;
using Favo.Books.Modules.Catalog.Application
    .Imports.GetById;
using Favo.Books.Modules.Catalog.Infrastructure
    .ExternalBooks.OpenLibrary;
using Favo.Books.Modules.Catalog.Infrastructure
    .Imports;
using Favo.Books.Modules.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Favo.Books.Modules.Catalog.Application.Books;
using Favo.Books.SharedKernel.Catalog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Favo.Books.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<INovelAchievementCatalog, NovelAchievementCatalog>();
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
        services.AddScoped<IDiscoveryCatalog, DiscoveryCatalog>();
        services.AddSingleton<IApprovedNovelCatalog, ApprovedNovelCatalog>();

        services.AddOptions<OpenLibraryOptions>()
            .Bind(
                configuration.GetSection(
                    OpenLibraryOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.BaseUrl,
                        UriKind.Absolute,
                        out _),
                "Open Library base URL must be absolute.")
            .Validate(
                options =>
                    options.TimeoutSeconds > 0,
                "Open Library timeout must be " +
                "greater than zero.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.UserAgent),
                "Open Library user agent is required.")
            .Validate(
                options =>
                    options.RequestsPerSecond
                        is >= 1 and <= 3,
                "Open Library requests per second " +
                "must be between 1 and 3.")
            .ValidateOnStart();

        services.AddOptions<
                CatalogImportJobWorkerOptions>()
            .Bind(
                configuration.GetSection(
                    CatalogImportJobWorkerOptions
                        .SectionName))
            .Validate(
                options =>
                    options.PollIntervalSeconds > 0,
                "Poll interval must be greater " +
                "than zero.")
            .Validate(
                options =>
                    options.DelayBetweenPagesSeconds >=
                    0,
                "Page delay cannot be negative.")
            .Validate(
                options =>
                    options.MaximumRetryCount > 0,
                "Maximum retry count must be " +
                "greater than zero.")
            .Validate(
                options =>
                    options.InitialRetryDelaySeconds >
                    0,
                "Initial retry delay must be " +
                "greater than zero.")
            .Validate(
                options =>
                    options.MaximumRetryDelaySeconds >=
                    options
                        .InitialRetryDelaySeconds,
                "Maximum retry delay must be " +
                "greater than or equal to the " +
                "initial retry delay.")
            .Validate(
                options =>
                    options
                        .InterruptedJobTimeoutMinutes >
                    0,
                "Interrupted job timeout must be " +
                "greater than zero.")
            .ValidateOnStart();

        services.AddSingleton<
            OpenLibraryRequestGate>();

        services.AddTransient<
            OpenLibraryRateLimitHandler>();

        services.AddHttpClient<
                IExternalBookSearchClient,
                OpenLibrarySearchClient>(
                ConfigureOpenLibraryClient)
            .AddHttpMessageHandler<
                OpenLibraryRateLimitHandler>();

        services.AddHttpClient<
                IExternalBookDetailsClient,
                OpenLibraryBookDetailsClient>(
                ConfigureOpenLibraryClient)
            .AddHttpMessageHandler<
                OpenLibraryRateLimitHandler>();

        services.AddHttpClient<
                IExternalBookEditionClient,
                OpenLibraryBookEditionClient>(
                ConfigureOpenLibraryClient)
            .AddHttpMessageHandler<
                OpenLibraryRateLimitHandler>();

        services.AddScoped<IBookCatalog, BookCatalog>();
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

        services.AddScoped<
            BrowseBooksHandler>();

        services.AddScoped<
            GetBookSummariesHandler>();

        services.AddScoped<
            GetCategoriesHandler>();

        bool workerEnabled =
            configuration.GetValue(
                $"{CatalogImportJobWorkerOptions
                    .SectionName}:WorkerEnabled",
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
