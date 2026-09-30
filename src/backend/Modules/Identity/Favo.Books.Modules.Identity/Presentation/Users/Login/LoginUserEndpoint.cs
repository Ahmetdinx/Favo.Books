using System.ComponentModel.DataAnnotations;
using Favo.Books.Modules.Identity.Application.Abstractions.Security;
using Favo.Books.Modules.Identity.Infrastructure.Authentication;

using Microsoft.Extensions.Options;
using Favo.Books.Modules.Identity.Application.Users.Login;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Identity.Presentation.Users.Login;

internal static class LoginUserEndpoint
{
    public static RouteGroupBuilder MapLoginUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapPost("/login", HandleAsync)
            .WithName("LoginUser")
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        LoginUserRequest request,
        LoginUserHandler handler,
        HttpContext httpContext,
        IOptions<RefreshTokenOptions> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        LoginUserCommand command = new(request.Email!.Trim(), request.Password!);
        LoginUserResult result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.Unauthorized();
        }
        IssuedRefreshToken refreshToken = result.RefreshToken!;

        httpContext.Response.Cookies.Append(
            refreshTokenOptions.Value.CookieName,
            refreshToken.Value,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/api/identity",
                Expires = refreshToken.ExpiresAtUtc
            });

        LoginUserResponse response = new(
            result.UserId!.Value,
            result.Email!,
            result.AccessToken!.Value,
            result.AccessToken.ExpiresAtUtc);

        return Results.Ok(response);
    }

    private static Dictionary<string, string[]> Validate(LoginUserRequest request)
    {
        Dictionary<string, string[]> errors = [];

        string? email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors["email"] = ["E-posta adresi zorunludur."];
        }
        else if (email.Length > 320 || !new EmailAddressAttribute().IsValid(email))
        {
            errors["email"] = ["Geçerli bir e-posta adresi girilmelidir."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["Parola zorunludur."];
        }
        else if (request.Password.Length > 128)
        {
            errors["password"] = ["Parola en fazla 128 karakter olabilir."];
        }

        return errors;
    }
}