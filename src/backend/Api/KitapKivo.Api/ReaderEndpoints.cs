using System.Security.Claims;
using KitapKivo.Modules.Identity.Application.Users;
using KitapKivo.Modules.Identity.Domain.Users;
using KitapKivo.Modules.Reading.Application;
using KitapKivo.Modules.Profiles.Application;
using KitapKivo.Modules.Community.Application;

namespace KitapKivo.Api;

// The host composes module-owned read models; modules never read each other's tables.
public static class ReaderEndpoints
{
    public static void MapReaderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/readers").WithTags("Readers");
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });
        group.MapGet("/me", async (ClaimsPrincipal principal, ReaderDirectory directory, CancellationToken ct) =>
        {
            if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            var settings = await directory.Settings(id, ct);
            return settings is null ? Results.Unauthorized() : Results.Ok(settings);
        }).RequireAuthorization();
        group.MapGet("/me/achievements", async (ClaimsPrincipal principal, ReaderAchievements achievements, CancellationToken ct) =>
        {
            if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            return Results.Ok(new { items = await achievements.Get(id, ct) });
        }).RequireAuthorization();
        group.MapPut("/me", async (UpdateReaderRequest request, ClaimsPrincipal principal, ReaderDirectory directory, CancellationToken ct) =>
        {
            if (!Guid.TryParse(principal.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            var result = await directory.Update(id, request.Nickname, request.IsPublic, ct, request.AvatarId);
            return result switch
            {
                "saved" => Results.Ok(await directory.Settings(id, ct)),
                "taken" => Results.Conflict(new { message = "Bu kullanıcı adı kullanılıyor." }),
                "invalid_avatar" => Results.BadRequest(new { message = "Listeden geçerli bir avatar seçin." }),
                "missing" => Results.Unauthorized(),
                _ => Results.BadRequest(new { message = UserNickname.Message })
            };
        }).RequireAuthorization();
        group.MapGet("/", async (string? query, int? page, ReaderDirectory directory, CancellationToken ct) =>
        {
            var term = query?.Trim().ToLowerInvariant() ?? "";
            if (term.Length < 2 || term.Length > 24 || term.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')) ||
                (page ?? 1) < 1 || (page ?? 1) > 500)
                return Results.BadRequest(new { message = "En az iki karakterle kullanıcı adı arayın." });
            var names = await directory.Search(term, page ?? 1, ct);
            return Results.Ok(new { items = names.Take(20), hasMore = names.Length > 20 });
        });
        group.MapGet("/{nickname}", async (string nickname, int? page, ReaderDirectory directory,
            PublicLibrary library, PublicShowcase showcase, PublicReaderRatings ratings, ReaderAchievements achievements, CancellationToken ct) =>
        {
            if ((page ?? 1) < 1 || (page ?? 1) > 50000) return Results.BadRequest();
            var normalized = nickname.Trim().ToLowerInvariant();
            var reader = await directory.FindPublic(normalized, ct);
            if (reader is null) return Results.NotFound();
            var books = await library.Get(reader.Id, page ?? 1, ct);
            var featured = await showcase.Get(reader.Id, ct);
            var ids = books.Items.Select(b => b.BookId).Concat(featured.InfluentialBooks.Select(b => b.BookId))
                .Concat(featured.BedsideBookId is Guid bedside ? new[] { bedside } : Array.Empty<Guid>()).Distinct().ToArray();
            var scores = await ratings.Get(reader.Id, ids, ct);
            var progress = await achievements.Get(reader.Id, ct);
            // Recheck after loading: closing/renaming a profile invalidates subsequent responses.
            if ((await directory.FindPublic(normalized, ct))?.Id != reader.Id) return Results.NotFound();
            return Results.Ok(new { reader.Nickname, reader.AvatarId, library = books, showcase = featured, ratings = scores, achievements = progress });
        });
    }

    public sealed record UpdateReaderRequest(string? Nickname, bool IsPublic, string? AvatarId = null);
}
