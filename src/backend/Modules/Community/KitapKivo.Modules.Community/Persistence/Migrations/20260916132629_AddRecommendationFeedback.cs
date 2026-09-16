using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapKivo.Modules.Community.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendationFeedback",
                schema: "community",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Choice = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationFeedback", x => new { x.UserId, x.BookId });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationFeedback",
                schema: "community");
        }
    }
}
