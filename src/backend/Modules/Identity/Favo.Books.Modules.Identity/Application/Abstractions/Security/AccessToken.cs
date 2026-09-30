namespace Favo.Books.Modules.Identity.Application.Abstractions.Security;

public sealed record AccessToken(
    string Value,
    DateTimeOffset ExpiresAtUtc);