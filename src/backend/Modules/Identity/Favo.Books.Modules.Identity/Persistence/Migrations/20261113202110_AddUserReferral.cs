using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReferral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralSource",
                schema: "identity",
                table: "users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferredById",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_ReferredById",
                schema: "identity",
                table: "users",
                column: "ReferredById");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_ReferredById",
                schema: "identity",
                table: "users",
                column: "ReferredById",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_ReferredById",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_ReferredById",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ReferralSource",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "ReferredById",
                schema: "identity",
                table: "users");
        }
    }
}
