using Favo.Books.Modules.Identity.Domain.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Identity.Persistence.Configurations;

internal sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.GoogleSubject).HasMaxLength(255);
        builder.HasIndex(user => user.GoogleSubject).IsUnique();

        builder.Property(user => user.Nickname).HasMaxLength(24);
        builder.HasIndex(user => user.Nickname).IsUnique();
        builder.Property(user => user.IsPublic).HasDefaultValue(false);
        builder.Property(user => user.IsAdmin).HasDefaultValue(false);
        builder.Property(user => user.AvatarId).HasMaxLength(16).HasDefaultValue("owl").IsRequired();
        builder.Property(user => user.ReferralSource).HasMaxLength(16);
        builder.HasIndex(user => user.ReferredById);
        builder.HasOne<User>().WithMany().HasForeignKey(user => user.ReferredById).OnDelete(DeleteBehavior.SetNull);

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();
    }
}
