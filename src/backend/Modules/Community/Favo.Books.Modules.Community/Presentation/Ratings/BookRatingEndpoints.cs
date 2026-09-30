using System.Security.Claims;
using Favo.Books.Modules.Community.Application.Ratings;
using Favo.Books.Modules.Community.Domain.Ratings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Community.Presentation.Ratings;

internal static class BookRatingEndpoints
{
    public static RouteGroupBuilder MapBookRatingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/me/rating-prompts", GetPromptsAsync)
            .WithName("GetMyRatingPrompts")
            .RequireAuthorization()
            .Produces<RatingPromptsResult>();
        group.MapGet("/books/{bookId:guid}/ratings", GetAsync)
            .WithName("GetBookRatings")
            .Produces<RatingResult>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/books/{bookId:guid}/rating", SetAsync)
            .WithName("SetBookRating")
            .RequireAuthorization()
            .Produces<RatingResult>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/books/{bookId:guid}/rating", RemoveAsync)
            .WithName("RemoveBookRating")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> GetPromptsAsync(
        ClaimsPrincipal user, HttpContext httpContext, GetRatingPromptsHandler handler,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        var userId = TryGetUserId(user);
        return userId.HasValue
            ? Results.Ok(await handler.HandleAsync(userId.Value, cancellationToken))
            : Results.Unauthorized();
    }

    private static async Task<IResult> GetAsync(
        Guid bookId,
        ClaimsPrincipal user,
        HttpContext httpContext,
        GetBookRatingsHandler handler,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        Guid? userId = TryGetUserId(user);
        RatingResult? result = await handler.HandleAsync(bookId, userId, cancellationToken);
        return result is null ? BookNotFound() : Results.Ok(result);
    }

    private static async Task<IResult> SetAsync(
        Guid bookId,
        SetRatingRequest request,
        ClaimsPrincipal user,
        SetBookRatingHandler handler,
        CancellationToken cancellationToken)
    {
        Guid? userId = TryGetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();
        Dictionary<string, string[]> errors = Validate(request);
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        var scores = new RatingScores(request.Theme!.Value, request.Language!.Value,
            request.Characters!.Value, request.Structure!.Value, request.Pace!.Value,
            request.Impact!.Value, request.Experience!.Value);

        RatingResult? result = await handler.HandleAsync(
            userId.Value, bookId, scores, cancellationToken);
        return result is null ? BookNotFound() : Results.Ok(result);
    }

    private static async Task<IResult> RemoveAsync(
        Guid bookId,
        ClaimsPrincipal user,
        RemoveBookRatingHandler handler,
        CancellationToken cancellationToken)
    {
        Guid? userId = TryGetUserId(user);
        if (!userId.HasValue) return Results.Unauthorized();
        bool removed = await handler.HandleAsync(userId.Value, bookId, cancellationToken);
        return removed ? Results.NoContent() : Results.NotFound();
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("sub")?.Value, out Guid userId) ? userId : null;

    private static IResult BookNotFound() => Results.Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Kitap bulunamadı.",
        detail: "Seçilen kitap katalogda bulunamadı.");

    internal static Dictionary<string, string[]> Validate(SetRatingRequest request)
    {
        Dictionary<string, string[]> errors = [];
        ValidateScore("theme", request.Theme);
        ValidateScore("language", request.Language);
        ValidateScore("characters", request.Characters);
        ValidateScore("structure", request.Structure);
        ValidateScore("pace", request.Pace);
        ValidateScore("impact", request.Impact);
        ValidateScore("experience", request.Experience);
        return errors;

        void ValidateScore(string key, int? value)
        {
            if (value is null)
                errors[key] = ["Bu kriterin puanı zorunludur."];
            else if (value is < RatingPolicy.MinimumScore or > RatingPolicy.MaximumScore)
                errors[key] = ["Puan 1 ile 10 arasında bir tam sayı olmalıdır."];
        }
    }

    internal sealed record SetRatingRequest(
        int? Theme, int? Language, int? Characters, int? Structure,
        int? Pace, int? Impact, int? Experience);
}
