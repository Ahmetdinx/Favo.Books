using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAdminFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAdmin",
                schema: "identity",
                table: "users");
        }
    }
}
