using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Favo.Books.Modules.Identity.Persistence.Migrations
{
    /// <summary>
    /// Replaces the animal avatars with ex libris seals. Every saved choice is moved to a seal so no
    /// profile falls back to the default; "owl" exists in both sets and is kept.
    /// </summary>
    public partial class ExLibrisAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE identity.users SET "AvatarId" = CASE "AvatarId"
                    WHEN 'fox' THEN 'lantern'
                    WHEN 'cat' THEN 'moon'
                    WHEN 'bear' THEN 'tree'
                    WHEN 'deer' THEN 'rose'
                    WHEN 'panda' THEN 'book'
                    WHEN 'penguin' THEN 'compass'
                    WHEN 'rabbit' THEN 'key'
                    ELSE "AvatarId" END
                WHERE "AvatarId" IN ('fox', 'cat', 'bear', 'deer', 'panda', 'penguin', 'rabbit');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Seals without an animal counterpart return to the default owl.
            migrationBuilder.Sql("""
                UPDATE identity.users SET "AvatarId" = CASE "AvatarId"
                    WHEN 'lantern' THEN 'fox'
                    WHEN 'moon' THEN 'cat'
                    WHEN 'tree' THEN 'bear'
                    WHEN 'rose' THEN 'deer'
                    WHEN 'book' THEN 'panda'
                    WHEN 'compass' THEN 'penguin'
                    WHEN 'key' THEN 'rabbit'
                    ELSE 'owl' END
                WHERE "AvatarId" <> 'owl';
                """);
        }
    }
}
