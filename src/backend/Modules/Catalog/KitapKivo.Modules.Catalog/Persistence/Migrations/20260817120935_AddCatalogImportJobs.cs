using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapKivo.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogImportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportJobs",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Query = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartPage = table.Column<int>(type: "integer", nullable: false),
                    NextPage = table.Column<int>(type: "integer", nullable: false),
                    PageSize = table.Column<int>(type: "integer", nullable: false),
                    PageCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedPageCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TotalAvailableCount = table.Column<int>(type: "integer", nullable: true),
                    ProcessedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedCount = table.Column<int>(type: "integer", nullable: false),
                    ExistingCount = table.Column<int>(type: "integer", nullable: false),
                    NotFoundCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                    table.CheckConstraint("CK_ImportJobs_Counts", "\"CompletedPageCount\" >= 0\r\nAND \"ProcessedCount\" >= 0\r\nAND \"CreatedCount\" >= 0\r\nAND \"ExistingCount\" >= 0\r\nAND \"NotFoundCount\" >= 0\r\nAND \"FailedCount\" >= 0\r\nAND \"ConsecutiveFailureCount\" >= 0");
                    table.CheckConstraint("CK_ImportJobs_PageValues", "\"StartPage\" >= 1\r\nAND \"NextPage\" >= 1\r\nAND \"PageSize\" BETWEEN 1 AND 20\r\nAND \"PageCount\" BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_ImportJobs_TotalAvailableCount", "\"TotalAvailableCount\" IS NULL\r\nOR \"TotalAvailableCount\" >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_CreatedAtUtc",
                schema: "catalog",
                table: "ImportJobs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ImportJobs_Status_NextAttemptAtUtc_CreatedAtUtc",
                schema: "catalog",
                table: "ImportJobs",
                columns: new[] { "Status", "NextAttemptAtUtc", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportJobs",
                schema: "catalog");
        }
    }
}
