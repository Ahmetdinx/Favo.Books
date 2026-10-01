using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReaderFollows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reader_follows",
                schema: "identity",
                columns: table => new
                {
                    FollowerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowedId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reader_follows", x => new { x.FollowerId, x.FollowedId });
                    table.CheckConstraint("CK_reader_follows_not_self", "\"FollowerId\" <> \"FollowedId\"");
                    table.ForeignKey(
                        name: "FK_reader_follows_users_FollowedId",
                        column: x => x.FollowedId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reader_follows_users_FollowerId",
                        column: x => x.FollowerId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reader_follows_FollowedId_CreatedAtUtc",
                schema: "identity",
                table: "reader_follows",
                columns: new[] { "FollowedId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reader_follows",
                schema: "identity");
        }
    }
}
