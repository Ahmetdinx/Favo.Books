using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using KitapKivo.Modules.Identity.Application.Abstractions.Security;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KitapKivo.Modules.Identity.Infrastructure.Authentication;

internal sealed class JwtAccessTokenProvider
    : IAccessTokenProvider
{
    private readonly JwtOptions _options;

    public JwtAccessTokenProvider(
        IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessToken Generate(
        Guid userId,
        string email)
    {
        DateTimeOffset issuedAtUtc =
            DateTimeOffset.UtcNow;

        DateTimeOffset expiresAtUtc =
            issuedAtUtc.AddMinutes(
                _options.ExpirationMinutes);

        Claim[] claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Email,
                email),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        ];

        byte[] secretBytes =
            Encoding.UTF8.GetBytes(_options.Secret);

        SymmetricSecurityKey securityKey =
            new(secretBytes);

        SigningCredentials signingCredentials =
            new(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token =
            new(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: issuedAtUtc.UtcDateTime,
                expires: expiresAtUtc.UtcDateTime,
                signingCredentials: signingCredentials);

        string tokenValue =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AccessToken(
            tokenValue,
            expiresAtUtc);
    }
}