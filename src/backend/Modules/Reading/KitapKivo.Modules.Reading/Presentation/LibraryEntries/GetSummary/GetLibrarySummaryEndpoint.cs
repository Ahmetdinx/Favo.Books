using System.Security.Claims;

using KitapKivo.Modules.Reading
    .Application.LibraryEntries.GetSummary;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Reading
    .Presentation.LibraryEntries.GetSummary;

internal static class GetLibrarySummaryEndpoint
{
    public static RouteGroupBuilder
        MapGetLibrarySummaryEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapGet(
                "/library/summary",
                HandleAsync)
            .WithName("GetLibrarySummary")
            .RequireAuthorization()
            .Produces<GetLibrarySummaryResponse>(
                StatusCodes.Status200OK)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult>
        HandleAsync(
            ClaimsPrincipal user,
            GetLibrarySummaryHandler handler,
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

        GetLibrarySummaryResult result =
            await handler.HandleAsync(
                new GetLibrarySummaryQuery(
                    userId),
                cancellationToken);

        GetLibrarySummaryResponse response =
            new(
                result.TotalCount,
                result.WantToReadCount,
                result.ReadingCount,
                result.ReadCount,
                result.OnHoldCount,
                result.DroppedCount);

        return Results.Ok(response);
    }
}