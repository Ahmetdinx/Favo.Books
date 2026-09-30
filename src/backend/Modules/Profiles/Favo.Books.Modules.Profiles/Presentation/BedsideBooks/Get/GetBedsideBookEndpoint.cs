using System.Security.Claims;

using Favo.Books.Modules.Profiles
    .Application.BedsideBooks.Get;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Profiles
    .Presentation.BedsideBooks.Get;

internal static class GetBedsideBookEndpoint
{
    public static RouteGroupBuilder
        MapGetBedsideBookEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapGet(
                "/me/bedside-book",
                HandleAsync)
            .WithName("GetBedsideBook")
            .RequireAuthorization()
            .Produces<GetBedsideBookResponse>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult>
        HandleAsync(
            ClaimsPrincipal user,
            GetBedsideBookHandler handler,
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

        GetBedsideBookResult? result =
            await handler.HandleAsync(
                new GetBedsideBookQuery(
                    userId),
                cancellationToken);

        if (result is null)
        {
            return Results.NotFound(new
            {
                message =
                    "Başucu kitabı seçilmemiş."
            });
        }

        GetBedsideBookResponse response =
            new(
                result.BedsideBookId,
                result.BookId,
                result.PersonalNote,
                result.CreatedAtUtc,
                result.UpdatedAtUtc);

        return Results.Ok(response);
    }
}