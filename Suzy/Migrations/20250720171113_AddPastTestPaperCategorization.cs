using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class AddPastTestPaperCategorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PastTestPaperCategories",
                columns: table => new
                {
                    PastTestPaperId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PastTestPaperCategories", x => new { x.PastTestPaperId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_PastTestPaperCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PastTestPaperCategories_PastTestPapers_PastTestPaperId",
                        column: x => x.PastTestPaperId,
                        principalTable: "PastTestPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PastTestPaperCategories_CategoryId",
                table: "PastTestPaperCategories",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PastTestPaperCategories");
        }
    }
}
