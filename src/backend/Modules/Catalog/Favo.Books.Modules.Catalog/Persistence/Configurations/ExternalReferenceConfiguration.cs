using Favo.Books.Modules.Catalog.Domain.Sources;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Catalog.Persistence.Configurations;

internal sealed class ExternalReferenceConfiguration :
    IEntityTypeConfiguration<ExternalReference>
{
    public void Configure(
        EntityTypeBuilder<ExternalReference> builder)
    {
        builder.ToTable(
            "ExternalReferences",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_ExternalReferences_ExactlyOneOwner",
                    """
                    num_nonnulls(
                        "BookId",
                        "AuthorId",
                        "EditionId",
                        "PublisherId"
                    ) = 1
                    """);
            });

        builder.HasKey(reference => reference.Id);

        builder.Property(reference => reference.Source)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(reference => reference.ExternalId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(reference => reference.SourceUrl)
            .HasMaxLength(2_000);

        builder.Property(reference =>
                reference.LastSynchronizedAtUtc)
            .IsRequired();

        builder.HasIndex(reference => new
        {
            reference.Source,
            reference.ExternalId
        }).IsUnique();

        builder.HasOne(reference => reference.Book)
            .WithMany(book => book.ExternalReferences)
            .HasForeignKey(reference => reference.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reference => reference.Author)
            .WithMany(author => author.ExternalReferences)
            .HasForeignKey(reference => reference.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reference => reference.Edition)
            .WithMany(edition => edition.ExternalReferences)
            .HasForeignKey(reference => reference.EditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(reference => reference.Publisher)
            .WithMany(publisher => publisher.ExternalReferences)
            .HasForeignKey(reference => reference.PublisherId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}