using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClearNonTurkishEditionCovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE catalog."Editions"
                SET "CoverImageUrl" = NULL
                WHERE LOWER(TRIM("LanguageCode")) NOT IN ('tr', 'tur')
                  AND "CoverImageUrl" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removed external cover URLs cannot be reconstructed safely.
        }
    }
}
