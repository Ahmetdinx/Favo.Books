using KitapKivo.Modules.Catalog.Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitapKivo.Modules.Catalog.Persistence.Configurations;

internal sealed class NovelReviewConfiguration : IEntityTypeConfiguration<NovelReview>
{
    public void Configure(EntityTypeBuilder<NovelReview> builder)
    {
        builder.ToTable("NovelReviews", t => t.HasCheckConstraint("CK_NovelReviews_Decision",
            "\"Decision\" IN ('Unknown', 'Novel', 'NotNovel', 'SelectedClassic')"));
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Decision).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.BatchId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.SourceUrl).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.EvidenceNote).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Reviewer).HasMaxLength(100).IsRequired();
        builder.Property(r => r.PolicyVersion).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => new { r.BookId, r.BatchId }).IsUnique();
        builder.HasIndex(r => new { r.BookId, r.Id }).IsDescending(false, true);
        builder.HasOne<Book>().WithMany(b => b.NovelReviews).HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
