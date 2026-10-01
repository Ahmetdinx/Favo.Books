using System.Security.Claims;
using Favo.Books.Modules.Identity.Application.Users;
using Favo.Books.Modules.Reading.Application;
using Favo.Books.Modules.Community.Application;

namespace Favo.Books.Api;

public static class SocialEndpoints
{
    public static void MapSocialEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/readers").WithTags("Reader network").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(context.HttpContext.User.FindFirstValue("sub"), out var id) || id == Guid.Empty)
                return Results.Unauthorized();
            return await next(context);
        });
        group.MapGet("/me/network", async (ClaimsPrincipal user, ReaderNetwork network, CancellationToken ct) =>
            Results.Ok(await network.State(Id(user), Id(user), ct)));
        group.MapGet("/me/connections", async (string? kind, int? page, ClaimsPrincipal user, ReaderNetwork network, CancellationToken ct) =>
        {
            if (kind is not ("followers" or "following") || (page ?? 1) < 1 || (page ?? 1) > 50000)
                return Results.BadRequest();
            var result = await network.List(Id(user), kind == "followers", page ?? 1, ct);
            return Results.Ok(new { items = result.Items.Select(r => new { r.Nickname, r.AvatarId }), result.HasMore });
        });
        group.MapGet("/{nickname}/network", async (string nickname, ClaimsPrincipal user, ReaderDirectory directory, ReaderNetwork network, CancellationToken ct) =>
        {
            var reader = await directory.FindPublic(Normalize(nickname), ct);
            return reader is null ? Results.NotFound() : Results.Ok(await network.State(reader.Id, Id(user), ct));
        });
        group.MapPut("/{nickname}/follow", async (string nickname, ClaimsPrincipal user, ReaderNetwork network, CancellationToken ct) =>
        {
            return await network.Follow(Id(user), Normalize(nickname), ct) switch
            {
                "saved" => Results.NoContent(),
                "missing" => Results.NotFound(),
                "private" => Results.Conflict(new { message = "Takip etmek için profil ayarlarından kitaplığını herkese aç." }),
                _ => Results.BadRequest(new { message = "Kendini takip edemezsin." })
            };
        });
        group.MapDelete("/{nickname}/follow", async (string nickname, ClaimsPrincipal user, ReaderNetwork network, CancellationToken ct) =>
        {
            await network.Unfollow(Id(user), Normalize(nickname), ct);
            return Results.NoContent();
        });
        group.MapGet("/me/activity", async (ClaimsPrincipal user, ReaderNetwork network, PublicLibrary library, PublicReaderRatings ratings, CancellationToken ct) =>
        {
            var id = Id(user);
            var following = await network.LinkedReaders(id, false, ct);
            var followers = await network.LinkedReaders(id, true, ct);
            var reading = await library.CurrentReading(following.Select(r => r.Id).ToArray(), ct);
            var reviews = await ratings.Recent(followers.Select(r => r.Id).ToArray(), ct);
            // Revalidate public visibility and relationship after composing module-owned data.
            var currentFollowing = (await network.LinkedReaders(id, false, ct)).ToDictionary(r => r.Id);
            var currentFollowers = (await network.LinkedReaders(id, true, ct)).ToDictionary(r => r.Id);
            return Results.Ok(new {
                reading = reading.Where(r => currentFollowing.ContainsKey(r.UserId)).Select(r => new {
                    currentFollowing[r.UserId].Nickname, currentFollowing[r.UserId].AvatarId, r.BookId, r.UpdatedAtUtc }),
                reviews = reviews.Where(r => currentFollowers.ContainsKey(r.UserId)).Select(r => new {
                    currentFollowers[r.UserId].Nickname, currentFollowers[r.UserId].AvatarId, r.BookId, r.OverallScore, r.UpdatedAtUtc })
            });
        });
    }
    private static Guid Id(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);
    private static string Normalize(string nickname) => nickname.Trim().ToLowerInvariant();
}
