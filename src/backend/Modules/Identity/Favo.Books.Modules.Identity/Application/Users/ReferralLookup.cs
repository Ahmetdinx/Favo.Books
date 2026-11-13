using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Identity.Application.Users;

/// <summary>Resolves the inviting reader's nickname at sign-up. Unknown or malformed nicknames are ignored, never an error.</summary>
internal static class ReferralLookup
{
    public static async Task ApplyAsync(IdentityDbContext db, User user, string? referrer, string? source, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(referrer) || referrer.Length > 24) return;
        string nickname;
        try { nickname = UserNickname.Normalize(referrer); }
        catch (ArgumentException) { return; }
        var referrerId = await db.Users.AsNoTracking().Where(u => u.Nickname == nickname).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(ct);
        if (referrerId is { } id) user.SetReferral(id, source);
    }
}
