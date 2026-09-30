using Favo.Books.Modules.Catalog.Domain.Authors;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Catalog.Persistence.Configurations;

internal sealed class AuthorConfiguration :
    IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");

        builder.HasKey(author => author.Id);

        builder.Property(author => author.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(author => author.Biography);

        builder.Property(author => author.BirthDate);

        builder.Property(author => author.DeathDate);

        builder.Property(author => author.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(author => author.Name);
    }
}