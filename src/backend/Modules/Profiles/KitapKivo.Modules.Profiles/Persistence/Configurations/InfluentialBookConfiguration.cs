using KitapKivo.Modules.Profiles
    .Domain.InfluentialBooks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore
    .Metadata.Builders;

namespace KitapKivo.Modules.Profiles
    .Persistence.Configurations;

internal sealed class
    InfluentialBookConfiguration :
        IEntityTypeConfiguration<
            InfluentialBook>
{
    public void Configure(
        EntityTypeBuilder<
            InfluentialBook> builder)
    {
        builder.ToTable(
            "InfluentialBooks",
            tableBuilder =>
                tableBuilder
                    .HasCheckConstraint(
                        "CK_InfluentialBooks_Position",
                        "\"Position\" BETWEEN 1 AND 5"));

        builder.HasKey(book => book.Id);

        builder.Property(book =>
                book.UserId)
            .IsRequired();

        builder.Property(book =>
                book.BookId)
            .IsRequired();

        builder.Property(book =>
                book.Position)
            .IsRequired();

        builder.Property(book =>
                book.PersonalReflection)
            .HasMaxLength(
                InfluentialBook
                    .MaximumPersonalReflectionLength);

        builder.Property(book =>
                book.CreatedAtUtc)
            .IsRequired();

        builder.Property(book =>
                book.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(book => new
        {
            book.UserId,
            book.Position
        }).IsUnique();

        builder.HasIndex(book => new
        {
            book.UserId,
            book.BookId
        }).IsUnique();

        builder.HasIndex(book =>
            book.BookId);
    }
}