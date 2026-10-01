using Favo.Books.Modules.Identity.Domain.Users;
using Favo.Books.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Favo.Books.Modules.Identity.Application.Users;

public sealed record NetworkState(int FollowerCount, int FollowingCount, bool IsFollowing, bool IsSelf);
public sealed record ConnectionPage(ReaderIdentity[] Items, bool HasMore);

public sealed class ReaderNetwork(IdentityDbContext db)
{
    private IQueryable<User> Visible => db.Users.AsNoTracking().Where(u => u.IsPublic && u.Nickname != null);
    private IQueryable<ReaderFollow> Follows => db.Set<ReaderFollow>().AsNoTracking();

    // Private profiles never appear in other readers' lists or activity feeds.
    private IQueryable<User> Linked(Guid id, bool followers) => Visible.Where(u => followers
        ? Follows.Any(f => f.FollowedId == id && f.FollowerId == u.Id)
        : Follows.Any(f => f.FollowerId == id && f.FollowedId == u.Id));

    public async Task<NetworkState> State(Guid id, Guid viewer, CancellationToken ct) => new(
        await Linked(id, true).CountAsync(ct), await Linked(id, false).CountAsync(ct),
        await Follows.AnyAsync(f => f.FollowerId == viewer && f.FollowedId == id, ct), id == viewer);

    public async Task<ConnectionPage> List(Guid id, bool followers, int page, CancellationToken ct)
    {
        var items = await Linked(id, followers).OrderBy(u => u.Nickname).ThenBy(u => u.Id)
            .Skip((page - 1) * 20).Take(21).Select(u => new ReaderIdentity(u.Id, u.Nickname!, u.AvatarId)).ToArrayAsync(ct);
        return new(items.Take(20).ToArray(), items.Length > 20);
    }

    public Task<ReaderIdentity[]> LinkedReaders(Guid id, bool followers, CancellationToken ct) =>
        Linked(id, followers).Select(u => new ReaderIdentity(u.Id, u.Nickname!, u.AvatarId)).ToArrayAsync(ct);

    public async Task<string> Follow(Guid viewer, string nickname, CancellationToken ct)
    {
        var target = await Visible.SingleOrDefaultAsync(u => u.Nickname == nickname, ct);
        if (target is null) return "missing";
        if (target.Id == viewer) return "self";
        if (!await Visible.AnyAsync(u => u.Id == viewer, ct)) return "private";
        // The unique pair plus ON CONFLICT makes repeated/concurrent follow requests idempotent.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO identity.reader_follows ("FollowerId", "FollowedId", "CreatedAtUtc")
            SELECT {viewer}, {target.Id}, {DateTimeOffset.UtcNow}
            WHERE EXISTS (SELECT 1 FROM identity.users WHERE "Id" = {target.Id} AND "IsPublic")
              AND EXISTS (SELECT 1 FROM identity.users WHERE "Id" = {viewer} AND "IsPublic")
            ON CONFLICT ("FollowerId", "FollowedId") DO NOTHING
            """, ct);
        return "saved";
    }

    public Task Unfollow(Guid viewer, string nickname, CancellationToken ct) => db.Set<ReaderFollow>()
        .Where(f => f.FollowerId == viewer && db.Users.Any(u => u.Id == f.FollowedId && u.Nickname == nickname))
        .ExecuteDeleteAsync(ct);
}
