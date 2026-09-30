using System.Security.Claims;
using Favo.Books.Modules.Reading.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Reading.Presentation.LibraryEntries;

internal static class LookupLibraryEntriesEndpoint
{
    public sealed record Request(Guid[]? BookIds);

    public static void MapLookupLibraryEntries(this RouteGroupBuilder group)
    {
        group.MapPost("/library/lookup", async (Request request, ClaimsPrincipal principal,
            ReadingDbContext db, HttpContext http, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var userId)) return Results.Unauthorized();
            if (request.BookIds is null || request.BookIds.Length > 50 || request.BookIds.Contains(Guid.Empty))
                return Results.BadRequest(new { message = "En fazla 50 geçerli kitap kimliği gönderilebilir." });
            var ids = request.BookIds.Distinct().ToArray();
            var entries = await db.LibraryEntries.AsNoTracking()
                .Where(e => e.UserId == userId && ids.Contains(e.BookId))
                .Select(e => new { LibraryEntryId = e.Id, e.BookId, e.Status, e.CreatedAtUtc, e.UpdatedAtUtc })
                .ToArrayAsync(ct);
            return Results.Ok(new { items = entries.Select(e => new {
                e.LibraryEntryId, e.BookId, Status = e.Status.ToString(), e.CreatedAtUtc, e.UpdatedAtUtc }) });
        }).RequireAuthorization();
    }
}
