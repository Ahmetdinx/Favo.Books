using KitapKivo.Modules.Community.Application.Ratings;
using KitapKivo.Modules.Community.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KitapKivo.Modules.Community;

public static class CommunityModule
{
    public static IServiceCollection AddCommunityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' was not found.");
        services.AddDbContext<CommunityDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<GetBookRatingsHandler>();
        services.AddScoped<SetBookRatingHandler>();
        services.AddScoped<RemoveBookRatingHandler>();
        services.AddScoped<GetRatingPromptsHandler>();
        services.AddScoped<BookRecommendations>();
        return services;
    }
}
