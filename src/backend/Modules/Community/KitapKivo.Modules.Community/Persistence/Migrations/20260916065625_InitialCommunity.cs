using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapKivo.Modules.Community.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "community");

            migrationBuilder.CreateTable(
                name: "BookRatings",
                schema: "community",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookRatings", x => x.Id);
                    table.CheckConstraint("CK_BookRatings_Score", "\"Score\" BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookRatings_BookId",
                schema: "community",
                table: "BookRatings",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookRatings_UserId_BookId",
                schema: "community",
                table: "BookRatings",
                columns: new[] { "UserId", "BookId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookRatings",
                schema: "community");
        }
    }
}
