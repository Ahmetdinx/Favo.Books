using System.Security.Claims;

using KitapKivo.Modules.Profiles
    .Application.BedsideBooks.Remove;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Profiles
    .Presentation.BedsideBooks.Remove;

internal static class RemoveBedsideBookEndpoint
{
    public static RouteGroupBuilder
        MapRemoveBedsideBookEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapDelete(
                "/me/bedside-book",
                HandleAsync)
            .WithName("RemoveBedsideBook")
            .RequireAuthorization()
            .Produces(
                StatusCodes.Status204NoContent)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult>
        HandleAsync(
            ClaimsPrincipal user,
            RemoveBedsideBookHandler handler,
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

        RemoveBedsideBookResult result =
            await handler.HandleAsync(
                new RemoveBedsideBookCommand(
                    userId),
                cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound(new
            {
                message =
                    "Başucu kitabı seçilmemiş."
            });
        }

        return Results.NoContent();
    }
}