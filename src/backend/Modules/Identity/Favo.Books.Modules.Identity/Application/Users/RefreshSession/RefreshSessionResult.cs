using Favo.Books.Modules.Identity
    .Application.Abstractions.Security;

namespace Favo.Books.Modules.Identity
    .Application.Users.RefreshSession;

public sealed record RefreshSessionResult(
    bool IsSuccess,
    Guid? UserId,
    string? Email,
    AccessToken? AccessToken,
    IssuedRefreshToken? RefreshToken)
{
    public static RefreshSessionResult Success(
        Guid userId,
        string email,
        AccessToken accessToken,
        IssuedRefreshToken refreshToken)
    {
        return new RefreshSessionResult(
            true,
            userId,
            email,
            accessToken,
            refreshToken);
    }

    public static RefreshSessionResult InvalidToken()
    {
        return new RefreshSessionResult(
            false,
            null,
            null,
            null,
            null);
    }
}