using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitapKivo.Modules.Community.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedBookRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_BookRatings_Score",
                schema: "community",
                table: "BookRatings");

            // Preserve the original votes; do not manufacture criterion scores.
            migrationBuilder.RenameColumn(
                name: "Score",
                schema: "community",
                table: "BookRatings",
                newName: "LegacyScore");

            migrationBuilder.AddColumn<int>(
                name: "Characters",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Experience",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Impact",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Language",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "LegacyScore",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "OverallScore",
                schema: "community",
                table: "BookRatings",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pace",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Structure",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Theme",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookRatings_CompleteScores",
                schema: "community",
                table: "BookRatings",
                sql: "(\n    \"LegacyScore\" IS NOT NULL AND \"LegacyScore\" BETWEEN 1 AND 5\n    AND \"Theme\" IS NULL AND \"Language\" IS NULL AND \"Characters\" IS NULL\n    AND \"Structure\" IS NULL AND \"Pace\" IS NULL AND \"Impact\" IS NULL\n    AND \"Experience\" IS NULL AND \"OverallScore\" IS NULL\n)\nOR\n(\n    \"LegacyScore\" IS NULL\n    AND \"Theme\" IS NOT NULL AND \"Theme\" BETWEEN 1 AND 10\n    AND \"Language\" IS NOT NULL AND \"Language\" BETWEEN 1 AND 10\n    AND \"Characters\" IS NOT NULL AND \"Characters\" BETWEEN 1 AND 10\n    AND \"Structure\" IS NOT NULL AND \"Structure\" BETWEEN 1 AND 10\n    AND \"Pace\" IS NOT NULL AND \"Pace\" BETWEEN 1 AND 10\n    AND \"Impact\" IS NOT NULL AND \"Impact\" BETWEEN 1 AND 10\n    AND \"Experience\" IS NOT NULL AND \"Experience\" BETWEEN 1 AND 10\n    AND \"OverallScore\" IS NOT NULL\n    AND \"OverallScore\" =\n        (15 * (\"Theme\" + \"Language\" + \"Characters\" + \"Structure\" + \"Impact\" + \"Experience\")\n         + 10 * \"Pace\") / 100.0\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Never discard seven-dimensional user input on an accidental rollback.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM community."BookRatings" WHERE "OverallScore" IS NOT NULL) THEN
                        RAISE EXCEPTION 'Cannot downgrade detailed ratings without an explicit data export and conversion plan.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookRatings_CompleteScores",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "Characters",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "Experience",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "Impact",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "Language",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "Pace",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "Structure",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.DropColumn(
                name: "Theme",
                schema: "community",
                table: "BookRatings");

            migrationBuilder.RenameColumn(
                name: "LegacyScore",
                schema: "community",
                table: "BookRatings",
                newName: "Score");

            migrationBuilder.AlterColumn<int>(
                name: "Score",
                schema: "community",
                table: "BookRatings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookRatings_Score",
                schema: "community",
                table: "BookRatings",
                sql: "\"Score\" BETWEEN 1 AND 5");
        }
    }
}
