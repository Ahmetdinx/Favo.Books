using Favo.Books.Modules.Catalog.Application
    .Imports.GetById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Favo.Books.SharedKernel.Authorization;

namespace Favo.Books.Modules.Catalog.Presentation
    .Imports.GetById;

internal static class GetCatalogImportJobByIdEndpoint
{
    public static RouteGroupBuilder
        MapGetCatalogImportJobByIdEndpoint(
            this RouteGroupBuilder group)
    {
        group.MapGet(
                "/books/external/import-jobs/{jobId:guid}",
                HandleAsync)
            .WithName("GetCatalogImportJobById")
            .RequireAuthorization(AuthorizationPolicies.CatalogAdmin)
            .Produces<GetCatalogImportJobByIdResponse>(
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
        Guid jobId,
        GetCatalogImportJobByIdHandler handler,
        CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["jobId"] =
                    [
                        "Import işi kimliği boş olamaz."
                    ]
                });
        }

        GetCatalogImportJobByIdResult? result =
            await handler.HandleAsync(
                new GetCatalogImportJobByIdQuery(
                    jobId),
                cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        GetCatalogImportJobByIdResponse response = new(
            result.JobId,
            result.Provider,
            result.Query,
            result.StartPage,
            result.NextPage,
            result.PageSize,
            result.PageCount,
            result.CompletedPageCount,
            result.Status,
            result.TotalAvailableCount,
            result.ProcessedCount,
            result.CreatedCount,
            result.ExistingCount,
            result.NotFoundCount,
            result.FailedCount,
            result.ConsecutiveFailureCount,
            result.LastError,
            result.NextAttemptAtUtc,
            result.CreatedAtUtc,
            result.UpdatedAtUtc,
            result.StartedAtUtc,
            result.CompletedAtUtc);

        return Results.Ok(response);
    }
}