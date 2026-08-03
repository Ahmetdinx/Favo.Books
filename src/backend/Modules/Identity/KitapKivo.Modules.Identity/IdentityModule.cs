using KitapKivo.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KitapKivo.Modules.Identity.Application.Abstractions.Security;
using KitapKivo.Modules.Identity.Infrastructure.Security;
using KitapKivo.Modules.Identity.Application.Users.Register;
using KitapKivo.Modules.Identity.Application.Users.Login;
using KitapKivo.Modules.Identity.Infrastructure.Authentication;

namespace KitapKivo.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Connection string 'Database' was not found.");

        services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddSingleton<IAccessTokenProvider,JwtAccessTokenProvider>();


        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        return services;
    }
}