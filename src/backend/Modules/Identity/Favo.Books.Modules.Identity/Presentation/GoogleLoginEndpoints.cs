using Favo.Books.Modules.Identity.Application.Abstractions.Security;
using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Domain.RefreshTokens;
using Favo.Books.Modules.Identity.Infrastructure.Authentication;
using Favo.Books.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Favo.Books.Modules.Identity;

internal static class GoogleLoginEndpoints
{
    private const string NonceCookie = "kitapkivo_google_nonce";
    internal sealed record GoogleRequest(string? Credential);
    private static CookieOptions Cookie => new() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict,
        Path = "/api/identity/google", IsEssential = true, MaxAge = TimeSpan.FromMinutes(5) };

    public static void MapGoogleLoginEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/google/config", (IConfiguration config, HttpContext context) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var clientId = config["Google:ClientId"];
            return Results.Ok(new { Enabled = !string.IsNullOrWhiteSpace(clientId), ClientId = clientId });
        });
        group.MapPost("/google/challenge", (IConfiguration config, HttpContext context, GoogleLoginChallenges challenges) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            if (string.IsNullOrWhiteSpace(config["Google:ClientId"])) return Results.NotFound();
            if (context.Request.Cookies.TryGetValue(NonceCookie, out var old)) challenges.Consume(old);
            var nonce = challenges.Create();
            context.Response.Cookies.Append(NonceCookie, nonce, Cookie);
            return Results.Ok(new { Nonce = nonce });
        });
        group.MapPost("/google", Login);
    }

    private static async Task<IResult> Login(GoogleRequest request, HttpContext context, IConfiguration config,
        GoogleLoginChallenges challenges, IGoogleIdentityValidator validator, IdentityDbContext db,
        IPasswordHasher passwordHasher, IAccessTokenProvider accessTokens, IRefreshTokenIssuer refreshTokens,
        IOptions<RefreshTokenOptions> options, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        var clientId = config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId)) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(request.Credential) || request.Credential.Length > 16384) return Results.BadRequest();
        if (!context.Request.Cookies.TryGetValue(NonceCookie, out var nonce) || nonce.Length != 64 || !challenges.Consume(nonce))
            return Results.Unauthorized();
        context.Response.Cookies.Delete(NonceCookie, Cookie);
        GoogleIdentity? identity;
        try { identity = await validator.ValidateAsync(request.Credential, clientId, nonce, ct); }
        catch (Exception error) when (error is HttpRequestException or InvalidOperationException or IOException)
        { return Results.Problem(statusCode: 503, title: "Google doğrulaması şu anda kullanılamıyor."); }
        if (identity is null) return Results.Unauthorized();
        var user = await db.Users.SingleOrDefaultAsync(u => u.GoogleSubject == identity.Subject, ct);
        if (user is null)
        {
            // Never link an existing local account based only on matching email.
            if (await db.Users.AnyAsync(u => u.Email == identity.Email, ct))
                return Results.Conflict(new { Message = "Bu e-posta ile bir hesabın var. E-posta ve parolanla giriş yap." });
            user = User.Create(identity.Email, passwordHasher.Hash(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48))), DateTimeOffset.UtcNow);
            user.SetGoogleSubject(identity.Subject);
            db.Users.Add(user);
        }
        var refresh = refreshTokens.Issue();
        db.RefreshTokens.Add(RefreshToken.Create(user.Id, refresh.Hash, refresh.CreatedAtUtc, refresh.ExpiresAtUtc));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        { return Results.Conflict(new { Message = "Hesap bilgileri değişti. Lütfen Google ile tekrar giriş yap." }); }
        var access = accessTokens.Generate(user.Id, user.Email);
        context.Response.Cookies.Append(options.Value.CookieName, refresh.Value, new CookieOptions {
            HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, IsEssential = true,
            Path = "/api/identity", Expires = refresh.ExpiresAtUtc });
        return Results.Ok(new { UserId = user.Id, user.Email, AccessToken = access.Value, ExpiresAtUtc = access.ExpiresAtUtc,
            NeedsNickname = user.Nickname is null });
    }
}
