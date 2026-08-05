using KitapKivo.Modules.Catalog.Domain.Publishers;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitapKivo.Modules.Catalog.Persistence.Configurations;

internal sealed class PublisherConfiguration :
    IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("Publishers");

        builder.HasKey(publisher => publisher.Id);

        builder.Property(publisher => publisher.Name)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(publisher => publisher.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(publisher => publisher.Name);
    }
}