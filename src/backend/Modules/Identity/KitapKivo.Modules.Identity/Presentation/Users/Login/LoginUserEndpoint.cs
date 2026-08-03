using System.ComponentModel.DataAnnotations;

using KitapKivo.Modules.Identity.Application.Users.Login;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Identity.Presentation.Users.Login;

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