using System.Security.Claims;

using KitapKivo.Modules.Profiles
    .Application.InfluentialBooks.Get;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Profiles
    .Presentation.InfluentialBooks.Get;

internal static class
    GetInfluentialBooksEndpoint
{
    public static RouteGroupBuilder
        MapGetInfluentialBooksEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapGet(
                "/me/influential-books",
                HandleAsync)
            .WithName("GetInfluentialBooks")
            .RequireAuthorization()
            .Produces<
                GetInfluentialBooksResponse>(
                    StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult>
        HandleAsync(
            ClaimsPrincipal user,
            GetInfluentialBooksHandler handler,
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

        GetInfluentialBooksResult result =
            await handler.HandleAsync(
                new GetInfluentialBooksQuery(
                    userId),
                cancellationToken);

        GetInfluentialBookItemResponse[] items =
            result.Items
                .Select(item =>
                    new
                        GetInfluentialBookItemResponse(
                            item.InfluentialBookId,
                            item.BookId,
                            item.Position,
                            item.PersonalReflection,
                            item.CreatedAtUtc,
                            item.UpdatedAtUtc))
                .ToArray();

        return Results.Ok(
            new GetInfluentialBooksResponse(
                items));
    }
}