using Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Get;
using Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Remove;
using Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Set;
using Favo.Books.Modules.Profiles.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Favo.Books.Modules.Profiles
    .Application.InfluentialBooks.Get;
using Favo.Books.Modules.Profiles
    .Application.InfluentialBooks.Remove;
using Favo.Books.Modules.Profiles
    .Application.InfluentialBooks.Set;
namespace Favo.Books.Modules.Profiles;

public static class ProfilesModule
{
    public static IServiceCollection
        AddProfilesModule(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentNullException.ThrowIfNull(
            configuration);

        string connectionString =
            configuration.GetConnectionString(
                "Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' " +
                "was not found.");

        services.AddDbContext<
            ProfilesDbContext>(
                options =>
                    options.UseNpgsql(
                        connectionString));

        services.AddScoped<
            SetBedsideBookHandler>();

        services.AddScoped<
            GetBedsideBookHandler>();

        services.AddScoped<
            RemoveBedsideBookHandler>();

        services.AddScoped<
    SetInfluentialBookHandler>();

        services.AddScoped<
            GetInfluentialBooksHandler>();

        services.AddScoped<
            RemoveInfluentialBookHandler>();

        return services;
    }
}