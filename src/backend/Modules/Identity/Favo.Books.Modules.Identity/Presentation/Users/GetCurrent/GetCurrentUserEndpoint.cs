using System.Security.Claims;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Identity.Presentation.Users.GetCurrent;

internal static class GetCurrentUserEndpoint
{
    public static RouteGroupBuilder MapGetCurrentUserEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/me", Handle)
            .WithName("GetCurrentUser")
            .RequireAuthorization()
            .Produces<GetCurrentUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static IResult Handle(ClaimsPrincipal user)
    {
        string? userIdValue = user.FindFirst("sub")?.Value;
        string? email = user.FindFirst("email")?.Value;

        if (!Guid.TryParse(userIdValue, out Guid userId) ||
            string.IsNullOrWhiteSpace(email))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new GetCurrentUserResponse(userId, email));
    }
}