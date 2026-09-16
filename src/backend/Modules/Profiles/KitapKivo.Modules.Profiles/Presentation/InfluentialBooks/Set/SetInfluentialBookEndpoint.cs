using System.Security.Claims;

using KitapKivo.Modules.Profiles
    .Application.InfluentialBooks.Set;
using KitapKivo.Modules.Profiles
    .Domain.InfluentialBooks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Profiles
    .Presentation.InfluentialBooks.Set;

internal static class
    SetInfluentialBookEndpoint
{
    public static RouteGroupBuilder
        MapSetInfluentialBookEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPut(
                "/me/influential-books/" +
                "{position:int}",
                HandleAsync)
            .WithName("SetInfluentialBook")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .Produces<
                SetInfluentialBookResponse>(
                    StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult>
        HandleAsync(
            int position,
            SetInfluentialBookRequest request,
            ClaimsPrincipal user,
            SetInfluentialBookHandler handler,
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

        if (position <
                InfluentialBook.MinimumPosition ||
            position >
                InfluentialBook.MaximumPosition)
        {
            errors["position"] =
            [
                "Pozisyon 1 ile 5 arasında " +
                "olmalıdır."
            ];
        }

        if (request.BookId == Guid.Empty)
        {
            errors["bookId"] =
                ["Kitap kimliği boş olamaz."];
        }

        string? reflection =
            request.PersonalReflection?.Trim();

        if (reflection?.Length >
            InfluentialBook
                .MaximumPersonalReflectionLength)
        {
            errors["personalReflection"] =
            [
                "Kişisel düşünce en fazla " +
                $"{InfluentialBook.MaximumPersonalReflectionLength} " +
                "karakter olabilir."
            ];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(
                errors);
        }

        SetInfluentialBookResult? result =
            await handler.HandleAsync(
                new SetInfluentialBookCommand(
                    userId,
                    request.BookId,
                    position,
                    request.PersonalReflection),
                cancellationToken);

        if (result is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Kitap bulunamadı.",
                detail: "Seçilen kitap katalogda bulunamadı.");
        }

        if (!result.IsSuccess)
        {
            return Results.Conflict(new
            {
                message =
                    "Bu kitap etkileyen kitaplar " +
                    "arasında zaten bulunuyor."
            });
        }

        SetInfluentialBookResponse response =
            new(
                result.InfluentialBookId
                    ?? throw new
                        InvalidOperationException(
                            "Influential book " +
                            "identifier was not returned."),
                result.BookId
                    ?? throw new
                        InvalidOperationException(
                            "Book identifier " +
                            "was not returned."),
                result.Position
                    ?? throw new
                        InvalidOperationException(
                            "Position was not returned."),
                result.PersonalReflection,
                result.CreatedAtUtc
                    ?? throw new
                        InvalidOperationException(
                            "Creation time was not returned."),
                result.UpdatedAtUtc
                    ?? throw new
                        InvalidOperationException(
                            "Update time was not returned."));

        return Results.Ok(response);
    }
}
