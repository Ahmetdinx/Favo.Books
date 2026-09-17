using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KitapKivo.Modules.Catalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNovelReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NovelReviews",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BatchId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EvidenceNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Reviewer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelReviews", x => x.Id);
                    table.CheckConstraint("CK_NovelReviews_Decision", "\"Decision\" IN ('Unknown', 'Novel', 'NotNovel')");
                    table.ForeignKey(
                        name: "FK_NovelReviews_Books_BookId",
                        column: x => x.BookId,
                        principalSchema: "catalog",
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NovelReviews_BookId_BatchId",
                schema: "catalog",
                table: "NovelReviews",
                columns: new[] { "BookId", "BatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NovelReviews_BookId_Id",
                schema: "catalog",
                table: "NovelReviews",
                columns: new[] { "BookId", "Id" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NovelReviews",
                schema: "catalog");
        }
    }
}
