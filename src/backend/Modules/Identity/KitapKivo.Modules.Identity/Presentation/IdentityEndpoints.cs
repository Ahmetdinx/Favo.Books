using KitapKivo.Modules.Identity.Presentation.Users.Login;
using KitapKivo.Modules.Identity.Presentation.Users.Register;
using KitapKivo.Modules.Identity.Presentation.Users.GetCurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using KitapKivo.Modules.Identity.Presentation.Users.RefreshSession;
using KitapKivo.Modules.Identity.Presentation.Users.LogoutSession;

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
        group.MapRefreshSessionEndpoint();
        group.MapLogoutSessionEndpoint();
        group.MapGetCurrentUserEndpoint();

        return app;
    }
}