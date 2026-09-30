using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReaderIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                schema: "identity",
                table: "users",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Nickname",
                schema: "identity",
                table: "users",
                column: "Nickname",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Nickname",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Nickname",
                schema: "identity",
                table: "users");
        }
    }
}
