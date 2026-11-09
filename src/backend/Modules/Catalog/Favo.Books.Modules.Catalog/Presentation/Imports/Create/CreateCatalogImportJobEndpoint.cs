using Favo.Books.Modules.Catalog.Application
    .Imports.Create;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Favo.Books.SharedKernel.Authorization;

namespace Favo.Books.Modules.Catalog.Presentation
    .Imports.Create;

internal static class CreateCatalogImportJobEndpoint
{
    private const string SupportedProvider =
        "OpenLibrary";

    public static RouteGroupBuilder
        MapCreateCatalogImportJobEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapPost(
                "/books/external/import-jobs",
                HandleAsync)
            .WithName("CreateCatalogImportJob")
            .RequireAuthorization(AuthorizationPolicies.CatalogAdmin)
            .Produces<CreateCatalogImportJobResponse>(
                StatusCodes.Status202Accepted)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> HandleAsync(
        CreateCatalogImportJobRequest request,
        CreateCatalogImportJobHandler handler,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors =
            Validate(request);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        CreateCatalogImportJobCommand command = new(
            request.Provider!.Trim(),
            request.Query!.Trim(),
            request.StartPage,
            request.PageSize,
            request.PageCount);

        CreateCatalogImportJobResult result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        string statusUrl =
            $"/api/catalog/books/external/" +
            $"import-jobs/{result.JobId}";

        CreateCatalogImportJobResponse response = new(
            result.JobId,
            result.Status,
            result.CreatedAtUtc,
            statusUrl);

        return Results.Accepted(
            statusUrl,
            response);
    }

    internal static Dictionary<string, string[]>
        Validate(
            CreateCatalogImportJobRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Dictionary<string, string[]> errors = [];

        string? provider = request.Provider?.Trim();

        if (string.IsNullOrWhiteSpace(provider))
        {
            errors["provider"] =
                ["Kitap sağlayıcısı zorunludur."];
        }
        else if (!string.Equals(
                     provider,
                     SupportedProvider,
                     StringComparison.OrdinalIgnoreCase))
        {
            errors["provider"] =
            [
                "Yalnızca OpenLibrary sağlayıcısı " +
                "desteklenmektedir."
            ];
        }

        string? query = request.Query?.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            errors["query"] =
                ["Arama metni zorunludur."];
        }
        else if (query.Length > 200)
        {
            errors["query"] =
                ["Arama metni 200 karakteri geçemez."];
        }

        if (request.StartPage < 1)
        {
            errors["startPage"] =
                ["Başlangıç sayfası en az 1 olmalıdır."];
        }

        if (request.PageSize is < 1 or > 20)
        {
            errors["pageSize"] =
            [
                "Sayfa büyüklüğü 1 ile 20 " +
                "arasında olmalıdır."
            ];
        }

        if (request.PageCount is < 1 or > 500)
        {
            errors["pageCount"] =
            [
                "Sayfa sayısı 1 ile 500 " +
                "arasında olmalıdır."
            ];
        }

        if (request.StartPage > 0 &&
            request.PageCount > 0 &&
            request.StartPage >
                int.MaxValue - request.PageCount)
        {
            errors["startPage"] =
            [
                "Başlangıç sayfası ile sayfa sayısı " +
                "geçerli aralığı aşmaktadır."
            ];
        }

        return errors;
    }
}