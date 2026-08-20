using KitapKivo.Modules.Reading
    .Application.LibraryEntries.Add;
using KitapKivo.Modules.Reading
    .Application.LibraryEntries.Get;
using KitapKivo.Modules.Reading
    .Application.LibraryEntries.GetByBook;
using KitapKivo.Modules.Reading
    .Application.LibraryEntries.GetSummary;
using KitapKivo.Modules.Reading
    .Application.LibraryEntries.Remove;
using KitapKivo.Modules.Reading
    .Application.LibraryEntries.UpdateStatus;
using KitapKivo.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Modules.Reading;

public static class ReadingModule
{
    public static IServiceCollection AddReadingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString(
                "Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' " +
                "was not found.");

        services.AddDbContext<ReadingDbContext>(
            options =>
                options.UseNpgsql(
                    connectionString));

        services.AddScoped<
            AddBookToLibraryHandler>();

        services.AddScoped<
            GetLibraryHandler>();

        services.AddScoped<
            GetLibrarySummaryHandler>();

        services.AddScoped<
            GetLibraryEntryByBookHandler>();

        services.AddScoped<
            UpdateLibraryStatusHandler>();

        services.AddScoped<
            RemoveBookFromLibraryHandler>();

        return services;
    }
}