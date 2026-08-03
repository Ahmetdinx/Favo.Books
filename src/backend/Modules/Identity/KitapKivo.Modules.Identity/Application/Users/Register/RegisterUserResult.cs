namespace KitapKivo.Modules.Identity.Application.Users.Register;

public enum RegisterUserError
{
    None = 0,
    EmailAlreadyExists = 1
}

public sealed record RegisterUserResult(
    Guid? UserId,
    RegisterUserError Error)
{
    public bool IsSuccess => Error == RegisterUserError.None;

    public static RegisterUserResult Success(Guid userId) =>
        new(userId, RegisterUserError.None);

    public static RegisterUserResult EmailAlreadyExists() =>
        new(null, RegisterUserError.EmailAlreadyExists);
}