using KitapKivo.Modules.Catalog.Presentation
    .Authors.Create;
using KitapKivo.Modules.Catalog.Presentation
    .Books.Browse;
using KitapKivo.Modules.Catalog.Presentation
    .Books.Create;
using KitapKivo.Modules.Catalog.Presentation
    .Books.GetById;
using KitapKivo.Modules.Catalog.Presentation
    .Books.GetExternalDetails;
using KitapKivo.Modules.Catalog.Presentation
    .Books.ResolveExternal;
using KitapKivo.Modules.Catalog.Presentation
    .Books.Search;
using KitapKivo.Modules.Catalog.Presentation
    .Categories.GetAll;
using KitapKivo.Modules.Catalog.Presentation
    .Imports.Create;
using KitapKivo.Modules.Catalog.Presentation
    .Imports.GetById;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder
        MapCatalogEndpoints(
            this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/catalog")
            .WithTags("Catalog");

        group.MapCreateAuthorEndpoint();
        group.MapCreateBookEndpoint();
        group.MapSearchBooksEndpoint();
        group.MapGetBookByIdEndpoint();
        group.MapGetExternalBookDetailsEndpoint();
        group.MapResolveExternalBookEndpoint();
        group.MapCreateCatalogImportJobEndpoint();
        group.MapGetCatalogImportJobByIdEndpoint();
        group.MapBrowseBooksEndpoint();
        group.MapGetCategoriesEndpoint();

        return app;
    }
}