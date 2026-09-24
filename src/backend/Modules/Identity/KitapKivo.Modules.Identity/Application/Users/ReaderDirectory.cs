using KitapKivo.Modules.Identity.Domain.Users;
using KitapKivo.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KitapKivo.Modules.Identity.Application.Users;

// Internal composition data. Public endpoints never serialize the User entity.
public sealed record ReaderIdentity(Guid Id, string Nickname);
public sealed record ReaderSettings(string? Nickname, bool IsPublic);
public sealed class ReaderDirectory(IdentityDbContext db)
{
    public Task<ReaderSettings?> Settings(Guid id, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new ReaderSettings(u.Nickname, u.IsPublic)).SingleOrDefaultAsync(ct);

    public Task<ReaderIdentity?> FindPublic(string nickname, CancellationToken ct) =>
        db.Users.AsNoTracking().Where(u => u.IsPublic && u.Nickname == nickname)
            .Select(u => new ReaderIdentity(u.Id, u.Nickname!)).SingleOrDefaultAsync(ct);

    public async Task<string[]> Search(string query, int page, CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => u.IsPublic && u.Nickname != null && u.Nickname.StartsWith(query))
            .OrderBy(u => u.Nickname).Skip((page - 1) * 20).Take(21).Select(u => u.Nickname!).ToArrayAsync(ct);

    public async Task<string> Update(Guid id, string? nickname, bool isPublic, CancellationToken ct)
    {
        string normalized;
        try { normalized = UserNickname.Normalize(nickname); }
        catch (ArgumentException) { return "invalid"; }
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return "missing";
        if (await db.Users.AnyAsync(u => u.Id != id && u.Nickname == normalized, ct)) return "taken";
        user.SetProfile(normalized, isPublic);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { return "taken"; }
        return "saved";
    }
}
