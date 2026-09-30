using Favo.Books.Modules.Catalog.Domain.Books;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Catalog.Persistence.Configurations;

internal sealed class BookConfiguration :
    IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(book => book.Id);

        builder.Property(book => book.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(book => book.OriginalTitle)
            .HasMaxLength(500);

        builder.Property(book => book.Description);

        builder.Property(book => book.OriginalLanguageCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(book => book.FirstPublishedYear);

        builder.Property(book => book.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(book => new { book.CreatedAtUtc, book.Title, book.Id })
            .IsDescending(true, false, false);
        builder.HasIndex(book => new { book.Title, book.Id });

        builder.HasIndex(book => book.OriginalTitle);
    }
}
