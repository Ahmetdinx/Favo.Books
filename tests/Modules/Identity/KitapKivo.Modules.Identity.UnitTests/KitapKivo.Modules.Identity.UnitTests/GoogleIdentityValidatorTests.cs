using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using KitapKivo.Modules.Identity.Infrastructure.Authentication;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace KitapKivo.Modules.Identity.UnitTests;

public sealed class GoogleIdentityValidatorTests
{
    [Theory]
    [InlineData("client", "https://accounts.google.com", "nonce", true, true)]
    [InlineData("wrong-client", "https://accounts.google.com", "nonce", true, false)]
    [InlineData("client", "https://evil.example", "nonce", true, false)]
    [InlineData("client", "https://accounts.google.com", "wrong-nonce", true, false)]
    [InlineData("client", "https://accounts.google.com", "nonce", false, false)]
    public async Task ValidatesSignedIdentity(string audience, string issuer, string nonce, bool verified, bool valid)
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        var config = new OpenIdConnectConfiguration(); config.SigningKeys.Add(key);
        var validator = new GoogleIdentityValidator(new StaticConfigurationManager<OpenIdConnectConfiguration>(config));
        var token = new JwtSecurityToken(issuer, audience, [new Claim("sub", "google-user"), new Claim("email", "reader@gmail.com"),
            new Claim("email_verified", verified ? "true" : "false", ClaimValueTypes.Boolean), new Claim("nonce", nonce)],
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5), new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        var result = await validator.ValidateAsync(new JwtSecurityTokenHandler().WriteToken(token), "client", "nonce", default);
        Assert.Equal(valid, result is not null);
    }
    [Fact]
    public void ChallengeCanOnlyBeConsumedOnce()
    {
        using var challenges = new GoogleLoginChallenges();
        var nonce = challenges.Create();
        Assert.False(challenges.Consume("unknown"));
        Assert.True(challenges.Consume(nonce));
        Assert.False(challenges.Consume(nonce));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectsExpiredOrUntrustedSignatures(bool expired)
    {
        using var trusted = RSA.Create(2048);
        using var foreign = RSA.Create(2048);
        var trustedKey = new RsaSecurityKey(trusted) { KeyId = "test" };
        var signingKey = expired ? trustedKey : new RsaSecurityKey(foreign) { KeyId = "test" };
        var config = new OpenIdConnectConfiguration(); config.SigningKeys.Add(trustedKey);
        var validator = new GoogleIdentityValidator(new StaticConfigurationManager<OpenIdConnectConfiguration>(config));
        var token = new JwtSecurityToken("https://accounts.google.com", "client",
            [new Claim("sub", "user"), new Claim("email", "a@gmail.com"), new Claim("nonce", "nonce"), new Claim("email_verified", "true", ClaimValueTypes.Boolean)],
            DateTime.UtcNow.AddHours(-2), expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256));
        Assert.Null(await validator.ValidateAsync(new JwtSecurityTokenHandler().WriteToken(token), "client", "nonce", default));
    }
}
