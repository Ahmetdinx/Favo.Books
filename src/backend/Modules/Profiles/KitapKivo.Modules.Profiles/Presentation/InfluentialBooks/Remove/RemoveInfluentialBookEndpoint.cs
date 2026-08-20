using System.Security.Claims;

using KitapKivo.Modules.Profiles
    .Application.InfluentialBooks.Remove;
using KitapKivo.Modules.Profiles
    .Domain.InfluentialBooks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Profiles
    .Presentation.InfluentialBooks.Remove;

internal static class
    RemoveInfluentialBookEndpoint
{
    public static RouteGroupBuilder
        MapRemoveInfluentialBookEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapDelete(
                "/me/influential-books/" +
                "{position:int}",
                HandleAsync)
            .WithName("RemoveInfluentialBook")
            .RequireAuthorization()
            .Produces(
                StatusCodes.Status204NoContent)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult>
        HandleAsync(
            int position,
            ClaimsPrincipal user,
            RemoveInfluentialBookHandler handler,
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

        if (position <
                InfluentialBook.MinimumPosition ||
            position >
                InfluentialBook.MaximumPosition)
        {
            return Results.ValidationProblem(
                new Dictionary<
                    string,
                    string[]>
                {
                    ["position"] =
                    [
                        "Pozisyon 1 ile 5 " +
                        "arasında olmalıdır."
                    ]
                });
        }

        RemoveInfluentialBookResult result =
            await handler.HandleAsync(
                new
                    RemoveInfluentialBookCommand(
                        userId,
                        position),
                cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound(new
            {
                message =
                    "Bu pozisyonda etkileyen " +
                    "kitap bulunamadı."
            });
        }

        return Results.NoContent();
    }
}