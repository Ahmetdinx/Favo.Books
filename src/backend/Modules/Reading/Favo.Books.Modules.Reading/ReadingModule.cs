using Favo.Books.Modules.Reading
    .Application.LibraryEntries.Add;
using Favo.Books.Modules.Reading
    .Application.LibraryEntries.Get;
using Favo.Books.Modules.Reading
    .Application.LibraryEntries.GetByBook;
using Favo.Books.Modules.Reading
    .Application.LibraryEntries.GetSummary;
using Favo.Books.Modules.Reading
    .Application.LibraryEntries.Remove;
using Favo.Books.Modules.Reading
    .Application.LibraryEntries.UpdateStatus;
using Favo.Books.Modules.Reading.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Modules.Reading;

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

        services.AddScoped<Favo.Books.SharedKernel.Reading.ICompletedBooks,
            Application.LibraryEntries.CompletedBooks>();
        services.AddScoped<Favo.Books.SharedKernel.Reading.IReadingHistory,
            Application.LibraryEntries.ReadingHistory>();

        return services;
    }
}
