using System.Text.RegularExpressions;

using Favo.Books.Modules.Catalog.Application
    .Abstractions.ExternalBooks;
using Favo.Books.Modules.Catalog.Application
    .Books.GetExternalDetails;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Favo.Books.Modules.Catalog.Presentation
    .Books.GetExternalDetails;

internal static class GetExternalBookDetailsEndpoint
{
    private const string SupportedProvider =
        "OpenLibrary";

    private static readonly Regex WorkIdPattern = new(
        @"^/?works/OL\d+W$",
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant);

    public static RouteGroupBuilder
        MapGetExternalBookDetailsEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapGet(
                "/books/external/details",
                HandleAsync)
            .WithName("GetExternalBookDetails")
            .Produces<GetExternalBookDetailsResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        string? provider,
        string? externalId,
        GetExternalBookDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors =
            Validate(provider, externalId);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        GetExternalBookDetailsQuery query = new(
            externalId!.Trim());

        ExternalBookDetails? details =
            await handler.HandleAsync(
                query,
                cancellationToken);

        if (details is null)
        {
            return Results.NotFound();
        }

        GetExternalBookAuthorResponse[] authors =
            details.Authors
                .Select(author =>
                    new GetExternalBookAuthorResponse(
                        author.ExternalId,
                        author.Name))
                .ToArray();

        GetExternalBookDetailsResponse response = new(
            details.Provider,
            details.ExternalId,
            details.Title,
            details.Description,
            authors,
            details.FirstPublishedYear,
            details.CoverUrl,
            details.Isbns.ToArray(),
            details.LanguageCodes.ToArray(),
            details.Subjects.ToArray());

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