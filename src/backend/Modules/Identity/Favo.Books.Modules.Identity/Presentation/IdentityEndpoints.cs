using Favo.Books.Modules.Identity.Presentation.Users.Login;
using Favo.Books.Modules.Identity.Presentation.Users.Register;
using Favo.Books.Modules.Identity.Presentation.Users.GetCurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Favo.Books.Modules.Identity.Presentation.Users.RefreshSession;
using Favo.Books.Modules.Identity.Presentation.Users.LogoutSession;

namespace Favo.Books.Modules.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/identity")
            .WithTags("Identity");

        group.MapGoogleLoginEndpoints();
        group.MapRegisterUserEndpoint();
        group.MapLoginUserEndpoint();
        group.MapRefreshSessionEndpoint();
        group.MapLogoutSessionEndpoint();
        group.MapGetCurrentUserEndpoint();

        return app;
    }
}