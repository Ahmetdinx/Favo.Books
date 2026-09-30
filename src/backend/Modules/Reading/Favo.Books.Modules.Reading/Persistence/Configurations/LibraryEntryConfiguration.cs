using Favo.Books.Modules.Reading.Domain.LibraryEntries;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Reading
    .Persistence.Configurations;

internal sealed class LibraryEntryConfiguration :
    IEntityTypeConfiguration<LibraryEntry>
{
    public void Configure(
        EntityTypeBuilder<LibraryEntry> builder)
    {
        builder.ToTable("LibraryEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.UserId)
            .IsRequired();

        builder.Property(entry => entry.BookId)
            .IsRequired();

        builder.Property(entry => entry.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(entry => entry.CreatedAtUtc)
            .IsRequired();

        builder.Property(entry => entry.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(entry => new
        {
            entry.UserId,
            entry.BookId
        }).IsUnique();

        builder.HasIndex(entry => new
        {
            entry.UserId,
            entry.Status
        });

        builder.HasIndex(entry => entry.BookId);
    }
}