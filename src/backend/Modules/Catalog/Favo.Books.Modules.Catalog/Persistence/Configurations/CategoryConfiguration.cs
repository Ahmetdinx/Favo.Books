using Favo.Books.Modules.Catalog.Domain.Categories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Favo.Books.Modules.Catalog.Persistence.Configurations;

internal sealed class CategoryConfiguration :
    IEntityTypeConfiguration<Category>
{
    public void Configure(
        EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.HasIndex(category => category.Slug)
            .IsUnique();

        DateTimeOffset seedDateUtc = new(
            2026,
            8,
            12,
            0,
            0,
            0,
            TimeSpan.Zero);

        builder.HasData(
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000001"),
                Name = "Roman",
                Slug = "roman",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000002"),
                Name = "Klasikler",
                Slug = "klasikler",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000003"),
                Name = "Bilim Kurgu",
                Slug = "bilim-kurgu",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000004"),
                Name = "Fantastik",
                Slug = "fantastik",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000005"),
                Name = "Polisiye ve Gizem",
                Slug = "polisiye-ve-gizem",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000006"),
                Name = "Tarih",
                Slug = "tarih",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000007"),
                Name = "Felsefe",
                Slug = "felsefe",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000008"),
                Name = "Psikoloji",
                Slug = "psikoloji",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000009"),
                Name = "Biyografi",
                Slug = "biyografi",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000010"),
                Name = "Şiir",
                Slug = "siir",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000011"),
                Name = "Çocuk",
                Slug = "cocuk",
                CreatedAtUtc = seedDateUtc
            },
            new
            {
                Id = Guid.Parse(
                    "10000000-0000-0000-0000-000000000012"),
                Name = "Kişisel Gelişim",
                Slug = "kisisel-gelisim",
                CreatedAtUtc = seedDateUtc
            });

        var novelGenres = new (string Name, string Slug)[]
        {
            ("Tarihî Roman", "tarihi-roman"), ("Psikolojik Roman", "psikolojik-roman"),
            ("Aşk ve Romantik", "ask-ve-romantik"), ("Macera", "macera"),
            ("Gerilim", "gerilim"), ("Korku", "korku"), ("Distopya", "distopya")
        };
        for (var i = 0; i < novelGenres.Length; i++)
            builder.HasData(new
            {
                Id = Guid.Parse($"10000000-0000-0000-0000-{i + 13:000000000000}"),
                Name = novelGenres[i].Name, Slug = novelGenres[i].Slug, CreatedAtUtc = seedDateUtc
            });
    }
}
