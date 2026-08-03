using KitapKivo.Modules.Identity.Presentation.Users.Login;
using KitapKivo.Modules.Identity.Presentation.Users.Register;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace KitapKivo.Modules.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/identity")
            .WithTags("Identity");

        group.MapRegisterUserEndpoint();
        group.MapLoginUserEndpoint();

        return app;
    }
}