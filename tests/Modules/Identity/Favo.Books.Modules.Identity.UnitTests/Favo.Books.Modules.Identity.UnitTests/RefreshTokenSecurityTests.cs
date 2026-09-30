using Favo.Books.Modules.Identity.Infrastructure.Authentication;
using Favo.Books.Modules.Identity.Application.Abstractions.Security;
using Microsoft.Extensions.Options;
namespace Favo.Books.Modules.Identity.UnitTests;

public sealed class RefreshTokenSecurityTests
{
    [Fact]
    public void Generate_ShouldCreateUniqueBase64UrlTokens()
    {
        var generator =
            new CryptographicRefreshTokenGenerator();

        string firstToken = generator.Generate();
        string secondToken = generator.Generate();

        Assert.Equal(86, firstToken.Length);
        Assert.NotEqual(firstToken, secondToken);

        Assert.DoesNotContain("=", firstToken);
        Assert.DoesNotContain("+", firstToken);
        Assert.DoesNotContain("/", firstToken);
    }

    [Fact]
    public void Hash_ShouldReturnExpectedSha256HexValue()
    {
        var hasher =
            new Sha256RefreshTokenHasher();

        string hash =
            hasher.Hash("kitapkivo-refresh-token");

        Assert.Equal(
            "22062BB54462A7E8E7416D43BFDA1F97F" +
            "D371CDD67ACD48F1FD0CA9822E08FC1",
            hash);

        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void Issue_ShouldCreateTokenWithConfiguredExpiration()
    {
        DateTimeOffset nowUtc =
            new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

        RefreshTokenOptions options = new()
        {
            ExpirationDays = 30,
            CookieName = "KitapKivo.RefreshToken"
        };

        var issuer = new RefreshTokenIssuer(
            new StubRefreshTokenGenerator(),
            new StubRefreshTokenHasher(),
            Options.Create(options),
            new FixedTimeProvider(nowUtc));

        IssuedRefreshToken token = issuer.Issue();

        Assert.Equal("raw-refresh-token", token.Value);
        Assert.Equal(new string('A', 64), token.Hash);
        Assert.Equal(nowUtc, token.CreatedAtUtc);
        Assert.Equal(
            nowUtc.AddDays(30),
            token.ExpiresAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Hash_ShouldThrow_WhenTokenIsBlank(
        string token)
    {
        var hasher =
            new Sha256RefreshTokenHasher();

        Assert.Throws<ArgumentException>(() =>
            hasher.Hash(token));
    }

    private sealed class StubRefreshTokenGenerator
    : IRefreshTokenGenerator
    {
        public string Generate()
        {
            return "raw-refresh-token";
        }
    }

    private sealed class StubRefreshTokenHasher
        : IRefreshTokenHasher
    {
        public string Hash(string token)
        {
            return new string('A', 64);
        }
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset nowUtc)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return nowUtc;
        }
    }
}