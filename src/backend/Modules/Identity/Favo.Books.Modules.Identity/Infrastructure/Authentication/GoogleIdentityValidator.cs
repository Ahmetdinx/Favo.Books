using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Favo.Books.Modules.Identity.Infrastructure.Authentication;

public sealed record GoogleIdentity(string Subject, string Email);
public interface IGoogleIdentityValidator
{
    Task<GoogleIdentity?> ValidateAsync(string credential, string clientId, string nonce, CancellationToken ct);
}

internal sealed class GoogleIdentityValidator : IGoogleIdentityValidator
{
    private readonly IConfigurationManager<OpenIdConnectConfiguration> configuration;
    public GoogleIdentityValidator() : this(new ConfigurationManager<OpenIdConnectConfiguration>(
        "https://accounts.google.com/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever())) { }
    internal GoogleIdentityValidator(IConfigurationManager<OpenIdConnectConfiguration> configuration) => this.configuration = configuration;

    public async Task<GoogleIdentity?> ValidateAsync(string credential, string clientId, string nonce, CancellationToken ct)
    {
        var config = await configuration.GetConfigurationAsync(ct);
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(credential,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true, ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                        ValidateAudience = true, ValidAudience = clientId,
                        ValidateIssuerSigningKey = true, IssuerSigningKeys = config.SigningKeys,
                        RequireSignedTokens = true, RequireExpirationTime = true, ValidateLifetime = true,
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256], ClockSkew = TimeSpan.FromSeconds(30)
                    }, out _);
                var subject = principal.FindFirstValue("sub");
                var email = principal.FindFirstValue("email");
                if (principal.FindFirstValue("nonce") != nonce || principal.FindFirstValue("email_verified") != "true"
                    || string.IsNullOrWhiteSpace(subject) || subject.Length > 255
                    || string.IsNullOrWhiteSpace(email) || email.Length > 320
                    || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)) return null;
                return new(subject, email.Trim().ToLowerInvariant());
            }
            catch (SecurityTokenSignatureKeyNotFoundException) when (attempt == 0)
            {
                configuration.RequestRefresh();
                config = await configuration.GetConfigurationAsync(ct);
            }
            catch (SecurityTokenException) { return null; }
            catch (ArgumentException) { return null; }
        }
        return null;
    }
}
