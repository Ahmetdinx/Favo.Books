using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Favo.Books.Modules.Identity;

internal static class IdentityRateLimits
{
    public const string Login = "identity-login";
    public const string Register = "identity-register";
    public const string Session = "identity-session";

    public static IServiceCollection AddIdentityRateLimits(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { Message = "Çok fazla deneme yapıldı. Biraz sonra tekrar dene." },
                    cancellationToken);
            };

            AddPerClientPolicy(options, Login, permits: 10, TimeSpan.FromMinutes(1));
            AddPerClientPolicy(options, Register, permits: 10, TimeSpan.FromMinutes(10));
            // Each open tab refreshes on load, so sessions get a wider allowance.
            AddPerClientPolicy(options, Session, permits: 60, TimeSpan.FromMinutes(1));
        });

    private static void AddPerClientPolicy(RateLimiterOptions options, string name, int permits, TimeSpan window) =>
        options.AddPolicy(name, context =>
        {
            // Real connections always have an address; only in-memory test servers do not.
            string? client = context.Connection.RemoteIpAddress?.ToString();

            return client is null
                ? RateLimitPartition.GetNoLimiter(string.Empty)
                : RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permits,
                    Window = window,
                    QueueLimit = 0
                });
        });
}
