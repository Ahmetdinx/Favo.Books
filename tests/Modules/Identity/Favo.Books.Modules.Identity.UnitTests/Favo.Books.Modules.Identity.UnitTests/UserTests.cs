using Favo.Books.Modules.Identity.Domain.Users;

using Xunit;

namespace Favo.Books.Modules.Identity.UnitTests;

public sealed class UserTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldCreateUser_WhenValuesAreValid()
    {
        // Arrange
        const string email = "  AHMET@KITAPKIVO.COM  ";
        const string passwordHash = "hashed-password";

        // Act
        User user = User.Create(
            email,
            passwordHash,
            CreatedAtUtc);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("ahmet@kitapkivo.com", user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(CreatedAtUtc, user.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrowArgumentException_WhenEmailIsBlank(
        string email)
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(
                email,
                "hashed-password",
                CreatedAtUtc));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_ShouldThrowArgumentException_WhenPasswordHashIsBlank(
        string passwordHash)
    {
        Assert.Throws<ArgumentException>(() =>
            User.Create(
                "ahmet@kitapkivo.com",
                passwordHash,
                CreatedAtUtc));
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenCreatedAtIsNotUtc()
    {
        // Arrange
        var nonUtcDate = new DateTimeOffset(
            2026,
            7,
            31,
            15,
            0,
            0,
            TimeSpan.FromHours(3));

        // Act
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() =>
                User.Create(
                    "ahmet@kitapkivo.com",
                    "hashed-password",
                    nonUtcDate));

        // Assert
        Assert.Equal("createdAtUtc", exception.ParamName);
    }
}