using System.Security.Claims;
using KitapKivo.Modules.Community.Application.Ratings;
using KitapKivo.SharedKernel.Catalog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Community.Presentation.Ratings;

internal static class RecommendationEndpoints
{
    internal sealed record FeedbackRequest(string? Choice);
    public static void MapRecommendationEndpoints(this RouteGroupBuilder group)
    {
        var personal = group.MapGroup("/me").RequireAuthorization();
        personal.MapPut("/recommendations/{bookId:guid}/feedback", async (Guid bookId, FeedbackRequest request, ClaimsPrincipal user, BookRecommendations service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            if (request.Choice is not ("interested" or "dismissed"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["choice"] = ["Geçerli bir geri bildirim seçin."] });
            return await service.SetFeedbackAsync(id, bookId, request.Choice, ct) ? Results.NoContent() : Results.NotFound();
        });
        personal.MapDelete("/recommendations/{bookId:guid}/feedback", async (Guid bookId, ClaimsPrincipal user, BookRecommendations service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            await service.ClearFeedbackAsync(id, bookId, ct);
            return Results.NoContent();
        });
        personal.MapGet("/reading-preferences", async (ClaimsPrincipal user, HttpContext context, BookRecommendations service, IDiscoveryCatalog catalog, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(user.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            var preference = await service.GetPreferencesAsync(id, ct);
            var availableCategories = await catalog.GetGenresAsync(ct);
            return Results.Ok(new { Categories = preference?.Categories ?? [], Criteria = preference?.Criteria ?? [],
                HasSavedPreferences = preference is not null, availableCategories });
        });
        personal.MapPut("/reading-preferences", async (PreferenceRequest request, ClaimsPrincipal user, BookRecommendations service, CancellationToken ct) =>
        {
            if (!Guid.TryParse(user.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            return await service.SavePreferencesAsync(id, request, ct) ? Results.NoContent()
                : Results.ValidationProblem(new Dictionary<string, string[]> { ["preferences"] = ["En fazla beş geçerli tür ve üç farklı kriter seçin."] });
        });
        personal.MapGet("/recommendations", async (ClaimsPrincipal user, HttpContext context, BookRecommendations service, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (!Guid.TryParse(user.FindFirst("sub")?.Value, out var id)) return Results.Unauthorized();
            return Results.Ok(await service.GetAsync(id, ct));
        });
    }
}
