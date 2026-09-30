using System.Security.Claims;

using Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Set;
using Favo.Books.Modules.Profiles
    .Domain.BedsideBooks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Profiles
    .Presentation.BedsideBooks.Set;

internal static class SetBedsideBookEndpoint
{
    public static RouteGroupBuilder
        MapSetBedsideBookEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPut(
                "/me/bedside-book",
                HandleAsync)
            .WithName("SetBedsideBook")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<SetBedsideBookResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult>
        HandleAsync(
            SetBedsideBookRequest request,
            ClaimsPrincipal user,
            SetBedsideBookHandler handler,
            CancellationToken cancellationToken)
    {
        string? userIdValue =
            user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(
                userIdValue,
                out Guid userId))
        {
            return Results.Unauthorized();
        }

        Dictionary<string, string[]> errors =
            [];

        if (request.BookId == Guid.Empty)
        {
            errors["bookId"] =
                ["Kitap kimliği boş olamaz."];
        }

        string? normalizedNote =
            request.PersonalNote?.Trim();

        if (normalizedNote?.Length >
            BedsideBook
                .MaximumPersonalNoteLength)
        {
            errors["personalNote"] =
            [
                "Kişisel not en fazla " +
                $"{BedsideBook.MaximumPersonalNoteLength} " +
                "karakter olabilir."
            ];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(
                errors);
        }

        SetBedsideBookCommand command =
            new(
                userId,
                request.BookId,
                request.PersonalNote);

        SetBedsideBookResult? result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (result is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kitap bulunamadı.",
                detail: "Seçilen kitap katalogda bulunamadı.");
        }

        SetBedsideBookResponse response =
            new(
                result.BedsideBookId,
                result.BookId,
                result.PersonalNote,
                result.CreatedAtUtc,
                result.UpdatedAtUtc);

        return Results.Ok(response);
    }
}
