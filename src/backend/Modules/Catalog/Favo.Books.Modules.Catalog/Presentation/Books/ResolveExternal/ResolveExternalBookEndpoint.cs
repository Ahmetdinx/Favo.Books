using System.Text.RegularExpressions;

using Favo.Books.Modules.Catalog.Application
    .Books.ResolveExternal;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Catalog.Presentation
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
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
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
            if (result.RejectionReason is not null)
                return Results.Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Kitap katalog ekleme koşullarını karşılamıyor.",
                    detail: "Kaynakla doğrulanmış roman ve kapaklı Türkçe baskı gerekiyor.",
                    extensions: new Dictionary<string, object?> { ["code"] = result.RejectionReason });
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
