using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapKivo.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeCatalogPaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_Title",
                schema: "catalog",
                table: "Books");

            migrationBuilder.CreateIndex(
                name: "IX_Books_CreatedAtUtc_Title_Id",
                schema: "catalog",
                table: "Books",
                columns: new[] { "CreatedAtUtc", "Title", "Id" },
                descending: new[] { true, false, false });

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title_Id",
                schema: "catalog",
                table: "Books",
                columns: new[] { "Title", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_CreatedAtUtc_Title_Id",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_Title_Id",
                schema: "catalog",
                table: "Books");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Title",
                schema: "catalog",
                table: "Books",
                column: "Title");
        }
    }
}
