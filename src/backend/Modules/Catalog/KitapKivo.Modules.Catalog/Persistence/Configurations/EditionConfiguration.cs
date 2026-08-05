using KitapKivo.Modules.Catalog.Domain.Editions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitapKivo.Modules.Catalog.Persistence.Configurations;

internal sealed class EditionConfiguration :
    IEntityTypeConfiguration<Edition>
{
    public void Configure(EntityTypeBuilder<Edition> builder)
    {
        builder.ToTable("Editions");

        builder.HasKey(edition => edition.Id);

        builder.Property(edition => edition.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(edition => edition.LanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(edition => edition.Format)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(edition => edition.Isbn10)
            .HasMaxLength(10);

        builder.Property(edition => edition.Isbn13)
            .HasMaxLength(13);

        builder.Property(edition => edition.PageCount);

        builder.Property(edition => edition.PublicationYear);

        builder.Property(edition => edition.CoverImageUrl)
            .HasMaxLength(2_000);

        builder.Property(edition => edition.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(edition => edition.Isbn10)
            .IsUnique()
            .HasFilter("\"Isbn10\" IS NOT NULL");

        builder.HasIndex(edition => edition.Isbn13)
            .IsUnique()
            .HasFilter("\"Isbn13\" IS NOT NULL");

        builder.HasIndex(edition => edition.Title);

        builder.HasOne(edition => edition.Book)
            .WithMany(book => book.Editions)
            .HasForeignKey(edition => edition.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(edition => edition.Publisher)
            .WithMany(publisher => publisher.Editions)
            .HasForeignKey(edition => edition.PublisherId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}