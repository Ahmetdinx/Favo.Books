using Favo.Books.Modules.Catalog.Domain.Categories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Catalog.Persistence.Configurations;

internal sealed class BookCategoryConfiguration :
    IEntityTypeConfiguration<BookCategory>
{
    public void Configure(
        EntityTypeBuilder<BookCategory> builder)
    {
        builder.ToTable("BookCategories");

        builder.HasKey(bookCategory => new
        {
            bookCategory.BookId,
            bookCategory.CategoryId
        });

        builder.HasOne(bookCategory => bookCategory.Book)
            .WithMany(book => book.Categories)
            .HasForeignKey(bookCategory => bookCategory.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bookCategory => bookCategory.Category)
            .WithMany(category => category.Books)
            .HasForeignKey(bookCategory => bookCategory.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(
            bookCategory => bookCategory.CategoryId);
    }
}