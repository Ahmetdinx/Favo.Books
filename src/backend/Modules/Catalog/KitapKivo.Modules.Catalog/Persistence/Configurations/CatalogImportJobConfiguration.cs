using KitapKivo.Modules.Catalog.Domain.Imports;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KitapKivo.Modules.Catalog.Persistence
    .Configurations;

internal sealed class CatalogImportJobConfiguration :
    IEntityTypeConfiguration<CatalogImportJob>
{
    public void Configure(
        EntityTypeBuilder<CatalogImportJob> builder)
    {
        builder.ToTable(
            "ImportJobs",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_ImportJobs_PageValues",
                    """
                    "StartPage" >= 1
                    AND "NextPage" >= 1
                    AND "PageSize" BETWEEN 1 AND 20
                    AND "PageCount" BETWEEN 1 AND 500
                    """);

                tableBuilder.HasCheckConstraint(
                    "CK_ImportJobs_Counts",
                    """
                    "CompletedPageCount" >= 0
                    AND "ProcessedCount" >= 0
                    AND "CreatedCount" >= 0
                    AND "ExistingCount" >= 0
                    AND "NotFoundCount" >= 0
                    AND "FailedCount" >= 0
                    AND "ConsecutiveFailureCount" >= 0
                    """);

                tableBuilder.HasCheckConstraint(
                    "CK_ImportJobs_TotalAvailableCount",
                    """
                    "TotalAvailableCount" IS NULL
                    OR "TotalAvailableCount" >= 0
                    """);
            });

        builder.HasKey(job => job.Id);

        builder.Property(job => job.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(job => job.Query)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(job => job.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(job => job.LastError)
            .HasMaxLength(2_000);

        builder.Property(job => job.CreatedAtUtc)
            .IsRequired();

        builder.Property(job => job.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(job => new
        {
            job.Status,
            job.NextAttemptAtUtc,
            job.CreatedAtUtc
        });

        builder.HasIndex(job =>
            job.CreatedAtUtc);
    }
}