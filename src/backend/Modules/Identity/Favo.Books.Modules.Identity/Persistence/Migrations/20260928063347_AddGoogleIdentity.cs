using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleSubject",
                schema: "identity",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_GoogleSubject",
                schema: "identity",
                table: "users",
                column: "GoogleSubject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_GoogleSubject",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "GoogleSubject",
                schema: "identity",
                table: "users");
        }
    }
}
