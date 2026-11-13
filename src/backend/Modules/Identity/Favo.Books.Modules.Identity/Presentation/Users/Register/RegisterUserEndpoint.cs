using System.ComponentModel.DataAnnotations;

using Favo.Books.Modules.Identity.Application.Users.Register;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Identity.Presentation.Users.Register;

internal static class RegisterUserEndpoint
{
    public static RouteGroupBuilder MapRegisterUserEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapPost("/register", HandleAsync)
            .RequireRateLimiting(IdentityRateLimits.Register)
            .WithName("RegisterUser")
            .Produces<RegisterUserResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        RegisterUserRequest request,
        RegisterUserHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors = Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        RegisterUserCommand command = new(
            request.Email!.Trim(),
            request.Password!,
            request.Nickname,
            request.Referrer,
            request.ReferralSource);

        RegisterUserResult result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (result.Error == RegisterUserError.EmailAlreadyExists)
        {
            return Results.Conflict(new
            {
                code = "email_already_exists",
                message = "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var."
            });
        }

        if (result.Error == RegisterUserError.NicknameAlreadyExists)
            return Results.Conflict(new { code = "nickname_taken", message = "Bu kullanıcı adı kullanılıyor." });

        Guid userId = result.UserId!.Value;

        return Results.Created(
            $"/api/identity/users/{userId}",
            new RegisterUserResponse(userId));
    }

    private static Dictionary<string, string[]> Validate(
        RegisterUserRequest request)
    {
        Dictionary<string, string[]> errors = [];

        try { Domain.Users.UserNickname.Normalize(request.Nickname); }
        catch (ArgumentException) { errors["nickname"] = [Domain.Users.UserNickname.Message]; }

        string? email = request.Email?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            errors["email"] =
                ["E-posta adresi zorunludur."];
        }
        else if (email.Length > 320 ||
                 !new EmailAddressAttribute().IsValid(email))
        {
            errors["email"] =
                ["Geçerli bir e-posta adresi girilmelidir."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] =
                ["Parola zorunludur."];
        }
        else if (request.Password.Length < 8)
        {
            errors["password"] =
                ["Parola en az 8 karakter olmalıdır."];
        }
        else if (request.Password.Length > 128)
        {
            errors["password"] =
                ["Parola en fazla 128 karakter olabilir."];
        }

        return errors;
    }
}
