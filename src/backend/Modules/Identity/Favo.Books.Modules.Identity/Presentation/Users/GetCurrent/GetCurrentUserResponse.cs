namespace Favo.Books.Modules.Identity.Presentation.Users.GetCurrent;

public sealed record GetCurrentUserResponse(Guid UserId, string Email);