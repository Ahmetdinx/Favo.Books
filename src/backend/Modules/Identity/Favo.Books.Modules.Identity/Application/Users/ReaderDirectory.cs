using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Identity.Application.Users;

// Internal composition data. Public endpoints never serialize the User entity.
public sealed record ReaderIdentity(Guid Id, string Nickname, string AvatarId);
public sealed record ReaderSettings(string? Nickname, bool IsPublic, string AvatarId);
public sealed class ReaderDirectory(IdentityDbContext db)
{
    public Task<ReaderSettings?> Settings(Guid id, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new ReaderSettings(u.Nickname, u.IsPublic, u.AvatarId)).SingleOrDefaultAsync(ct);

    public Task<ReaderIdentity?> FindPublic(string nickname, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(u => u.IsPublic && u.Nickname == nickname)
            .Select(u => new ReaderIdentity(u.Id, u.Nickname!, u.AvatarId)).SingleOrDefaultAsync(ct);

    public async Task<string[]> Search(string query, int page, CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => u.IsPublic && u.Nickname != null && u.Nickname.StartsWith(query))
            .OrderBy(u => u.Nickname).Skip((page - 1) * 20).Take(21).Select(u => u.Nickname!).ToArrayAsync(ct);

    public async Task<string> Update(Guid id, string? nickname, bool isPublic, CancellationToken ct, string? avatarId = null)
    {
        string normalized;
        try { normalized = UserNickname.Normalize(nickname); }
        catch (ArgumentException) { return "invalid"; }
        if (avatarId is not null && !User.IsValidAvatar(avatarId)) return "invalid_avatar";
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return "missing";
        if (await db.Users.AnyAsync(u => u.Id != id && u.Nickname == normalized, ct)) return "taken";
        user.SetProfile(normalized, isPublic);
        if (avatarId is not null) user.SetAvatar(avatarId);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { return "taken"; }
        return "saved";
    }
}
