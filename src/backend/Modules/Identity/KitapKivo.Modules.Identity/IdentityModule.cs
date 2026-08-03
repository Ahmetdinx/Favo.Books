using KitapKivo.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KitapKivo.Modules.Identity.Application.Abstractions.Security;
using KitapKivo.Modules.Identity.Infrastructure.Security;
using KitapKivo.Modules.Identity.Application.Users.Register;
using KitapKivo.Modules.Identity.Application.Users.Login;
using KitapKivo.Modules.Identity.Infrastructure.Authentication;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

        JwtOptions jwtOptions = configuration
    .GetRequiredSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT ayarları bulunamadı.");

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
            string.IsNullOrWhiteSpace(jwtOptions.Secret) ||
            jwtOptions.ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT ayarları geçerli değil.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Secret)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        services.AddSingleton<IAccessTokenProvider,JwtAccessTokenProvider>();


        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        return services;
    }
}