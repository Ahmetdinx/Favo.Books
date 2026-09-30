using System.Security.Claims;

using Favo.Books.Modules.Reading
    .Application.LibraryEntries.UpdateStatus;
using Favo.Books.Modules.Reading
    .Domain.LibraryEntries;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Reading
    .Presentation.LibraryEntries.UpdateStatus;

internal static class UpdateLibraryStatusEndpoint
{
    public static RouteGroupBuilder MapUpdateLibraryStatusEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapPatch(
                "/library/{libraryEntryId:guid}/status",
                HandleAsync)
            .WithName("UpdateLibraryStatus")
            .RequireAuthorization()
            .Produces<UpdateLibraryStatusResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid libraryEntryId,
        UpdateLibraryStatusRequest request,
        ClaimsPrincipal user,
        UpdateLibraryStatusHandler handler,
        CancellationToken cancellationToken)
    {
        string? userIdValue =
            user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseStatus(
                request.Status,
                out LibraryEntryStatus status))
        {
            Dictionary<string, string[]> errors = new()
            {
                ["status"] =
                [
                    "Durum WantToRead, Reading, Read, " +
                    "OnHold veya Dropped olmalıdır."
                ]
            };

            return Results.ValidationProblem(errors);
        }

        UpdateLibraryStatusCommand command = new(
            userId,
            libraryEntryId,
            status);

        UpdateLibraryStatusResult result =
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

        Guid updatedLibraryEntryId =
            result.LibraryEntryId
            ?? throw new InvalidOperationException(
                "Library entry identifier was not returned.");

        LibraryEntryStatus updatedStatus =
            result.Status
            ?? throw new InvalidOperationException(
                "Updated library status was not returned.");

        DateTimeOffset updatedAtUtc =
            result.UpdatedAtUtc
            ?? throw new InvalidOperationException(
                "Updated time was not returned.");

        UpdateLibraryStatusResponse response = new(
            updatedLibraryEntryId,
            updatedStatus.ToString(),
            updatedAtUtc);

        return Results.Ok(response);
    }

    private static bool TryParseStatus(
        string? value,
        out LibraryEntryStatus status)
    {
        status = default;

        string? normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue) ||
            int.TryParse(normalizedValue, out _))
        {
            return false;
        }

        return Enum.TryParse(
                   normalizedValue,
                   ignoreCase: true,
                   out status) &&
               Enum.IsDefined(status);
    }
}