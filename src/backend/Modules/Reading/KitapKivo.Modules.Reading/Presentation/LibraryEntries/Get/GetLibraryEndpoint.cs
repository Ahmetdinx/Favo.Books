using System.Security.Claims;

using KitapKivo.Modules.Reading
    .Application.LibraryEntries.Get;
using KitapKivo.Modules.Reading
    .Domain.LibraryEntries;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Reading
    .Presentation.LibraryEntries.Get;

internal static class GetLibraryEndpoint
{
    public static RouteGroupBuilder MapGetLibraryEndpoint(
        this RouteGroupBuilder group)
    {
        group.MapGet("/library", HandleAsync)
            .WithName("GetLibrary")
            .RequireAuthorization()
            .Produces<GetLibraryResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string? status,
        int? page,
        int? pageSize,
        ClaimsPrincipal user,
        GetLibraryHandler handler,
        CancellationToken cancellationToken)
    {
        string? userIdValue =
            user.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdValue, out Guid userId))
        {
            return Results.Unauthorized();
        }

        int requestedPage = page ?? 1;
        int requestedPageSize = pageSize ?? 20;

        Dictionary<string, string[]> errors = [];

        if (requestedPage < 1)
        {
            errors["page"] =
                ["Sayfa numarası en az 1 olmalıdır."];
        }

        if (requestedPageSize is < 1 or > 100)
        {
            errors["pageSize"] =
                ["Sayfa boyutu 1 ile 100 arasında olmalıdır."];
        }

        LibraryEntryStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseStatus(
                    status,
                    out LibraryEntryStatus parsedStatus))
            {
                errors["status"] =
                [
                    "Durum WantToRead, Reading, Read, " +
                    "OnHold veya Dropped olmalıdır."
                ];
            }
            else
            {
                statusFilter = parsedStatus;
            }
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        GetLibraryQuery query = new(
            userId,
            statusFilter,
            requestedPage,
            requestedPageSize);

        GetLibraryResult result =
            await handler.HandleAsync(
                query,
                cancellationToken);

        GetLibraryItemResponse[] items =
            result.Items
                .Select(item =>
                    new GetLibraryItemResponse(
                        item.LibraryEntryId,
                        item.BookId,
                        item.Status.ToString(),
                        item.CreatedAtUtc,
                        item.UpdatedAtUtc))
                .ToArray();

        GetLibraryResponse response = new(
            result.TotalCount,
            result.Page,
            result.PageSize,
            items);

        return Results.Ok(response);
    }

    private static bool TryParseStatus(
        string value,
        out LibraryEntryStatus status)
    {
        status = default;

        string normalizedValue = value.Trim();

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