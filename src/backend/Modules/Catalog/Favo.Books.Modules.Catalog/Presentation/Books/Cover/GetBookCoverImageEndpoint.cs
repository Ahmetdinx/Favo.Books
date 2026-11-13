using Favo.Books.Modules.Catalog.Application.Books.Cover;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Catalog.Presentation.Books.Cover;

internal static class GetBookCoverImageEndpoint
{
    public static RouteGroupBuilder MapGetBookCoverImageEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/books/{bookId:guid}/cover", HandleAsync)
            .WithName("GetBookCoverImage")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid bookId, BookCoverImage covers, HttpContext context, CancellationToken cancellationToken)
    {
        BookCoverImageResult? cover;
        try { cover = await covers.GetAsync(bookId, cancellationToken); }
        catch (HttpRequestException) { return Results.StatusCode(StatusCodes.Status502BadGateway); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { return Results.StatusCode(StatusCodes.Status504GatewayTimeout); }
        if (cover is null) return Results.NotFound();
        context.Response.Headers.CacheControl = "public, max-age=86400";
        return Results.File(cover.Content, cover.ContentType);
    }
}
