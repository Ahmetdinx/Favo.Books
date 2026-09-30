using Favo.Books.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Favo.Books.Modules.Identity.Application.Abstractions.Security;
using Favo.Books.Modules.Identity.Infrastructure.Security;
using Favo.Books.Modules.Identity.Application.Users.Register;
using Favo.Books.Modules.Identity.Application.Users.Login;
using Favo.Books.Modules.Identity.Infrastructure.Authentication;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Favo.Books.Modules.Identity.Application.Users.RefreshSession;
using Favo.Books.Modules.Identity.Application.Users.LogoutSession;
namespace Favo.Books.Modules.Identity;

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

        services.AddOptions<RefreshTokenOptions>().Bind(configuration.GetRequiredSection(RefreshTokenOptions.SectionName)).Validate(options =>
         options.ExpirationDays is >= 1 and <= 90,
     "Refresh token süresi 1-90 gün arasında olmalıdır.").Validate(options =>
         !string.IsNullOrWhiteSpace(options.CookieName),
     "Refresh token cookie adı zorunludur.").ValidateOnStart();


        services.AddAuthorization();
        services.AddSingleton<IGoogleIdentityValidator, GoogleIdentityValidator>();
        services.AddSingleton<GoogleLoginChallenges>();

        services.AddSingleton<IAccessTokenProvider,JwtAccessTokenProvider>();

        services.AddSingleton<TimeProvider>(TimeProvider.System);

        services.AddSingleton<IRefreshTokenIssuer,RefreshTokenIssuer>();

        services.AddSingleton<IRefreshTokenGenerator,CryptographicRefreshTokenGenerator>();

        services.AddSingleton<IRefreshTokenHasher,Sha256RefreshTokenHasher>();

        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<LogoutSessionHandler>();
        return services;
    }
}