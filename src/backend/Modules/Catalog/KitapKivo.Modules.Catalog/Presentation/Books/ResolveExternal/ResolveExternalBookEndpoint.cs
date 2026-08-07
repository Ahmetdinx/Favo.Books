using System.Text.RegularExpressions;

using KitapKivo.Modules.Catalog.Application
    .Books.ResolveExternal;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog.Presentation
    .Books.ResolveExternal;

internal static class ResolveExternalBookEndpoint
{
    private const string SupportedProvider =
        "OpenLibrary";

    private static readonly Regex WorkIdPattern = new(
        @"^/?works/OL\d+W$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    public static RouteGroupBuilder
        MapResolveExternalBookEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPost(
                "/books/external/resolve",
                HandleAsync)
            .WithName("ResolveExternalBook")
            .RequireAuthorization()
            .Produces<ResolveExternalBookResponse>(
                StatusCodes.Status201Created)
            .Produces<ResolveExternalBookResponse>(
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
        ResolveExternalBookRequest request,
        ResolveExternalBookHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors =
            Validate(
                request.Provider,
                request.ExternalId);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        ResolveExternalBookCommand command = new(
            request.Provider!.Trim(),
            request.ExternalId!.Trim());

        ResolveExternalBookResult result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return Results.NotFound();
        }

        Guid bookId =
            result.BookId
            ?? throw new InvalidOperationException(
                "Book identifier was not returned.");

        ResolveExternalBookResponse response = new(
            bookId,
            result.IsCreated);

        if (result.IsCreated)
        {
            return Results.Created(
                $"/api/catalog/books/{bookId}",
                response);
        }

        return Results.Ok(response);
    }

    internal static Dictionary<string, string[]> Validate(
        string? provider,
        string? externalId)
    {
        Dictionary<string, string[]> errors = [];

        string? normalizedProvider =
            provider?.Trim();

        if (string.IsNullOrWhiteSpace(
                normalizedProvider))
        {
            errors["provider"] =
                ["Kitap sağlayıcısı zorunludur."];
        }
        else if (!string.Equals(
                     normalizedProvider,
                     SupportedProvider,
                     StringComparison.OrdinalIgnoreCase))
        {
            errors["provider"] =
                ["Yalnızca OpenLibrary sağlayıcısı desteklenmektedir."];
        }

        string? normalizedExternalId =
            externalId?.Trim();

        if (string.IsNullOrWhiteSpace(
                normalizedExternalId))
        {
            errors["externalId"] =
                ["Dış kitap kimliği zorunludur."];
        }
        else if (!WorkIdPattern.IsMatch(
                     normalizedExternalId))
        {
            errors["externalId"] =
            [
                "Open Library kitap kimliği " +
                "'/works/OL...W' biçiminde olmalıdır."
            ];
        }

        return errors;
    }
}