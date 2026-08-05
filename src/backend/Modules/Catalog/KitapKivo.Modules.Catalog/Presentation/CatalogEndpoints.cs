using KitapKivo.Modules.Catalog.Presentation.Authors.Create;
using KitapKivo.Modules.Catalog.Presentation.Books.Create;
using KitapKivo.Modules.Catalog.Presentation.Books.GetById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(
        this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/catalog")
            .WithTags("Catalog");

        group.MapCreateAuthorEndpoint();
        group.MapCreateBookEndpoint();
        group.MapGetBookByIdEndpoint();

        return app;
    }
}