using KitapKivo.Modules.Identity
    .Application.Abstractions.Security;
using KitapKivo.Modules.Identity
    .Application.Users.RefreshSession;
using KitapKivo.Modules.Identity
    .Infrastructure.Authentication;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace KitapKivo.Modules.Identity
    .Presentation.Users.RefreshSession;

internal static class RefreshSessionEndpoint
{
    public static RouteGroupBuilder
        MapRefreshSessionEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPost("/refresh", HandleAsync)
            .WithName("RefreshSession")
            .Produces<RefreshSessionResponse>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        return group;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        RefreshSessionHandler handler,
        IOptions<RefreshTokenOptions> options,
        CancellationToken cancellationToken)
    {
        RefreshTokenOptions refreshTokenOptions =
            options.Value;

        string rawRefreshToken =
            httpContext.Request.Cookies[
                refreshTokenOptions.CookieName]
            ?? string.Empty;

        RefreshSessionResult result =
            await handler.HandleAsync(
                new RefreshSessionCommand(
                    rawRefreshToken),
                cancellationToken);

        if (!result.IsSuccess)
        {
            DeleteRefreshTokenCookie(
                httpContext.Response,
                refreshTokenOptions);

            return Results.Unauthorized();
        }

        IssuedRefreshToken refreshToken =
            result.RefreshToken!;

        httpContext.Response.Cookies.Append(
            refreshTokenOptions.CookieName,
            refreshToken.Value,
            CreateCookieOptions(
                refreshToken.ExpiresAtUtc));

        RefreshSessionResponse response =
            new(
                result.UserId!.Value,
                result.Email!,
                result.AccessToken!.Value,
                result.AccessToken.ExpiresAtUtc);

        return Results.Ok(response);
    }

    private static CookieOptions CreateCookieOptions(
        DateTimeOffset expiresAtUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/api/identity",
            Expires = expiresAtUtc
        };
    }

    private static void DeleteRefreshTokenCookie(
        HttpResponse response,
        RefreshTokenOptions options)
    {
        response.Cookies.Delete(
            options.CookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/api/identity"
            });
    }
}