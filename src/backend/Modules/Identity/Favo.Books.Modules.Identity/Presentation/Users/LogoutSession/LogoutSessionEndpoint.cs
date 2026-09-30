using Favo.Books.Modules.Identity
    .Application.Users.LogoutSession;
using Favo.Books.Modules.Identity
    .Infrastructure.Authentication;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Favo.Books.Modules.Identity
    .Presentation.Users.LogoutSession;

internal static class LogoutSessionEndpoint
{
    public static RouteGroupBuilder
        MapLogoutSessionEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPost("/logout", HandleAsync)
            .WithName("LogoutSession")
            .Produces(
                StatusCodes.Status204NoContent)
            .AllowAnonymous();

        return group;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        LogoutSessionHandler handler,
        IOptions<RefreshTokenOptions> options,
        CancellationToken cancellationToken)
    {
        RefreshTokenOptions refreshTokenOptions =
            options.Value;

        string rawRefreshToken =
            httpContext.Request.Cookies[
                refreshTokenOptions.CookieName]
            ?? string.Empty;

        await handler.HandleAsync(
            new LogoutSessionCommand(
                rawRefreshToken),
            cancellationToken);

        httpContext.Response.Cookies.Delete(
            refreshTokenOptions.CookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/api/identity"
            });

        return Results.NoContent();
    }
}