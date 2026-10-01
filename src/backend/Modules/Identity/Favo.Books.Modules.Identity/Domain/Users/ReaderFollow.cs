using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Identity.Domain.Users;

public sealed class ReaderFollow
{
    public Guid FollowerId { get; private set; }
    public Guid FollowedId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}

internal sealed class ReaderFollowConfiguration : IEntityTypeConfiguration<ReaderFollow>
{
    public void Configure(EntityTypeBuilder<ReaderFollow> builder)
    {
        builder.ToTable("reader_follows", "identity", table =>
            table.HasCheckConstraint("CK_reader_follows_not_self", "\"FollowerId\" <> \"FollowedId\""));
        builder.HasKey(f => new { f.FollowerId, f.FollowedId });
        builder.HasIndex(f => new { f.FollowedId, f.CreatedAtUtc });
        builder.HasOne<User>().WithMany().HasForeignKey(f => f.FollowerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(f => f.FollowedId).OnDelete(DeleteBehavior.Cascade);
    }
}
