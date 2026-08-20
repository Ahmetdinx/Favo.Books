using KitapKivo.Modules.Profiles
    .Domain.BedsideBooks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore
    .Metadata.Builders;

namespace KitapKivo.Modules.Profiles
    .Persistence.Configurations;

internal sealed class
    BedsideBookConfiguration :
        IEntityTypeConfiguration<BedsideBook>
{
    public void Configure(
        EntityTypeBuilder<BedsideBook> builder)
    {
        builder.ToTable("BedsideBooks");

        builder.HasKey(book => book.Id);

        builder.Property(book =>
                book.UserId)
            .IsRequired();

        builder.Property(book =>
                book.BookId)
            .IsRequired();

        builder.Property(book =>
                book.PersonalNote)
            .HasMaxLength(
                BedsideBook
                    .MaximumPersonalNoteLength);

        builder.Property(book =>
                book.CreatedAtUtc)
            .IsRequired();

        builder.Property(book =>
                book.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(book =>
                book.UserId)
            .IsUnique();

        builder.HasIndex(book =>
            book.BookId);
    }
}