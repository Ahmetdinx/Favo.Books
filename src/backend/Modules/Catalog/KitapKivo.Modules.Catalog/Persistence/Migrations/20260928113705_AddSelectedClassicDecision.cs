using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapKivo.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedClassicDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_NovelReviews_Decision",
                schema: "catalog",
                table: "NovelReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NovelReviews_Decision",
                schema: "catalog",
                table: "NovelReviews",
                sql: "\"Decision\" IN ('Unknown', 'Novel', 'NotNovel', 'SelectedClassic')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_NovelReviews_Decision",
                schema: "catalog",
                table: "NovelReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NovelReviews_Decision",
                schema: "catalog",
                table: "NovelReviews",
                sql: "\"Decision\" IN ('Unknown', 'Novel', 'NotNovel')");
        }
    }
}
