using System.Security.Claims;
using KitapKivo.Modules.Reading.Domain.LibraryEntries;
using KitapKivo.Modules.Reading.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Reading.Presentation.LibraryEntries;

internal static class UndoLibraryChangeEndpoint
{
    public sealed record Request(DateTimeOffset? ExpectedUpdatedAtUtc, string? PreviousStatus);
    public static void MapUndoLibraryChange(this RouteGroupBuilder group)
    {
        group.MapPost("/library/{entryId:guid}/undo", async (Guid entryId, Request request,
            ClaimsPrincipal principal, ReadingDbContext db, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId)) return Results.Unauthorized();
            LibraryEntryStatus previous = default;
            if (request.ExpectedUpdatedAtUtc is null || entryId == Guid.Empty ||
                (request.PreviousStatus is not null && (int.TryParse(request.PreviousStatus, out _) ||
                    !Enum.TryParse(request.PreviousStatus, out previous) || !Enum.IsDefined(previous))))
                return Results.BadRequest();
            // Compare and mutate in the same SQL statement: a newer change must never be undone.
            var query = db.LibraryEntries.Where(e => e.UserId == userId && e.Id == entryId
                && e.UpdatedAtUtc == request.ExpectedUpdatedAtUtc);
            int count;
            if (request.PreviousStatus is null)
                count = await query.ExecuteDeleteAsync(ct);
            else
            {
                var now = DateTimeOffset.UtcNow;
                count = await query.ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, previous)
                    .SetProperty(e => e.UpdatedAtUtc, now), ct);
            }
            return count == 1 ? Results.NoContent() : Results.Conflict(new { message = "Kitaplık kaydı değişmiş. Son durum yenilendi." });
        }).RequireAuthorization();
    }
}
