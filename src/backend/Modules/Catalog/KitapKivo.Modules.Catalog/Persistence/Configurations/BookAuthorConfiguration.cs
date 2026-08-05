using KitapKivo.Modules.Catalog.Domain.Books;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitapKivo.Modules.Catalog.Persistence.Configurations;

internal sealed class BookAuthorConfiguration :
    IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("BookAuthors");

        builder.HasKey(bookAuthor => new
        {
            bookAuthor.BookId,
            bookAuthor.AuthorId
        });

        builder.Property(bookAuthor => bookAuthor.DisplayOrder)
            .IsRequired();

        builder.HasIndex(bookAuthor => new
        {
            bookAuthor.BookId,
            bookAuthor.DisplayOrder
        }).IsUnique();

        builder.HasOne(bookAuthor => bookAuthor.Book)
            .WithMany(book => book.Authors)
            .HasForeignKey(bookAuthor => bookAuthor.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bookAuthor => bookAuthor.Author)
            .WithMany(author => author.Books)
            .HasForeignKey(bookAuthor => bookAuthor.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}