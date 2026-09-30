using Favo.Books.Modules.Identity.Domain.RefreshTokens;

namespace Favo.Books.Modules.Identity.UnitTests;

public sealed class RefreshTokenTests
{
    private static readonly Guid UserId =
        Guid.Parse("9cba619a-6994-4b5f-a6f4-074abf55afdd");

    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly string TokenHash =
        new('A', 64);

    [Fact]
    public void Create_ShouldCreateActiveToken_WhenValuesAreValid()
    {
        RefreshToken token = RefreshToken.Create(
            UserId,
            TokenHash,
            CreatedAtUtc,
            CreatedAtUtc.AddDays(30));

        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.NotEqual(Guid.Empty, token.FamilyId);
        Assert.Equal(UserId, token.UserId);
        Assert.Equal(TokenHash, token.TokenHash);
        Assert.Null(token.RevokedAtUtc);
        Assert.Null(token.ReplacedByTokenId);
        Assert.True(token.IsActive(CreatedAtUtc));
    }

    [Fact]
    public void Create_ShouldThrow_WhenUserIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                Guid.Empty,
                TokenHash,
                CreatedAtUtc,
                CreatedAtUtc.AddDays(30)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-hash")]
    public void Create_ShouldThrow_WhenTokenHashIsInvalid(
        string tokenHash)
    {
        Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                UserId,
                tokenHash,
                CreatedAtUtc,
                CreatedAtUtc.AddDays(30)));
    }

    [Fact]
    public void Create_ShouldThrow_WhenCreatedAtIsNotUtc()
    {
        DateTimeOffset nonUtcDate =
            CreatedAtUtc.ToOffset(TimeSpan.FromHours(3));

        Assert.Throws<ArgumentException>(() =>
            RefreshToken.Create(
                UserId,
                TokenHash,
                nonUtcDate,
                CreatedAtUtc.AddDays(30)));
    }

    [Fact]
    public void Create_ShouldThrow_WhenExpirationIsNotAfterCreation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RefreshToken.Create(
                UserId,
                TokenHash,
                CreatedAtUtc,
                CreatedAtUtc));
    }

    [Fact]
    public void IsActive_ShouldReturnFalse_WhenTokenHasExpired()
    {
        RefreshToken token = CreateToken();

        Assert.False(
            token.IsActive(CreatedAtUtc.AddDays(30)));
    }

    [Fact]
    public void Revoke_ShouldMakeTokenInactive()
    {
        RefreshToken token = CreateToken();

        DateTimeOffset revokedAtUtc =
            CreatedAtUtc.AddHours(1);

        token.Revoke(revokedAtUtc);

        Assert.Equal(revokedAtUtc, token.RevokedAtUtc);
        Assert.False(token.IsActive(revokedAtUtc));
    }

    [Fact]
    public void Rotate_ShouldRevokeCurrentTokenAndPreserveFamily()
    {
        RefreshToken currentToken = CreateToken();

        DateTimeOffset rotatedAtUtc =
            CreatedAtUtc.AddHours(1);

        string replacementHash =
            new('B', 64);

        RefreshToken replacement =
            currentToken.Rotate(
                replacementHash,
                rotatedAtUtc,
                rotatedAtUtc.AddDays(30));

        Assert.Equal(
            replacement.Id,
            currentToken.ReplacedByTokenId);

        Assert.Equal(
            rotatedAtUtc,
            currentToken.RevokedAtUtc);

        Assert.Equal(
            currentToken.UserId,
            replacement.UserId);

        Assert.Equal(
            currentToken.FamilyId,
            replacement.FamilyId);

        Assert.True(
            replacement.IsActive(rotatedAtUtc));
    }

    [Fact]
    public void Rotate_ShouldThrow_WhenTokenIsAlreadyRevoked()
    {
        RefreshToken token = CreateToken();

        token.Revoke(CreatedAtUtc.AddMinutes(30));

        Assert.Throws<InvalidOperationException>(() =>
            token.Rotate(
                new string('B', 64),
                CreatedAtUtc.AddHours(1),
                CreatedAtUtc.AddDays(30)));
    }

    private static RefreshToken CreateToken()
    {
        return RefreshToken.Create(
            UserId,
            TokenHash,
            CreatedAtUtc,
            CreatedAtUtc.AddDays(30));
    }
}