using System.Security.Claims;

using Favo.Books.Modules.Reading
    .Application.LibraryEntries.Remove;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.Remove;

internal static class RemoveBookFromLibraryEndpoint
{
    public static RouteGroupBuilder MapRemoveBookFromLibraryEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapDelete(
                "/library/{libraryEntryId:guid}",
                HandleAsync)
            .WithName("RemoveBookFromLibrary")
            .RequireAuthorization()
            .Produces(
                StatusCodes.Status204NoContent)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid libraryEntryId,
        ClaimsPrincipal user,
        RemoveBookFromLibraryHandler handler,
        CancellationToken cancellationToken)
    {
        string? userIdValue =
            user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            return Results.Unauthorized();
        }

        RemoveBookFromLibraryCommand command = new(
            userId,
            libraryEntryId);

        RemoveBookFromLibraryResult result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound(new
            {
                message = "Kütüphane kaydı bulunamadı."
            });
        }

        return Results.NoContent();
    }
}