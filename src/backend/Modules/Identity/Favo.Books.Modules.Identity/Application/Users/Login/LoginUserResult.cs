using Favo.Books.Modules.Identity
    .Application.Abstractions.Security;

namespace Favo.Books.Modules.Identity
    .Application.Users.Login;

public sealed record LoginUserResult(
    bool IsSuccess,
    Guid? UserId,
    string? Email,
    AccessToken? AccessToken,
    IssuedRefreshToken? RefreshToken)
{
    public static LoginUserResult Success(
        Guid userId,
        string email,
        AccessToken accessToken,
        IssuedRefreshToken refreshToken)
    {
        return new LoginUserResult(
            true,
            userId,
            email,
            accessToken,
            refreshToken);
    }

    public static LoginUserResult InvalidCredentials()
    {
        return new LoginUserResult(
            false,
            null,
            null,
            null,
            null);
    }
}