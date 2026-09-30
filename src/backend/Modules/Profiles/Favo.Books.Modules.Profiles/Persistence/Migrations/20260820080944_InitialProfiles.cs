using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Profiles.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "profiles");

            migrationBuilder.CreateTable(
                name: "BedsideBooks",
                schema: "profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonalNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BedsideBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InfluentialBooks",
                schema: "profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    PersonalReflection = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfluentialBooks", x => x.Id);
                    table.CheckConstraint("CK_InfluentialBooks_Position", "\"Position\" BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BedsideBooks_BookId",
                schema: "profiles",
                table: "BedsideBooks",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BedsideBooks_UserId",
                schema: "profiles",
                table: "BedsideBooks",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfluentialBooks_BookId",
                schema: "profiles",
                table: "InfluentialBooks",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_InfluentialBooks_UserId_BookId",
                schema: "profiles",
                table: "InfluentialBooks",
                columns: new[] { "UserId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfluentialBooks_UserId_Position",
                schema: "profiles",
                table: "InfluentialBooks",
                columns: new[] { "UserId", "Position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BedsideBooks",
                schema: "profiles");

            migrationBuilder.DropTable(
                name: "InfluentialBooks",
                schema: "profiles");
        }
    }
}
