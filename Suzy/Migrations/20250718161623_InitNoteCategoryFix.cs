using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class InitNoteCategoryFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Notes");

            migrationBuilder.AddColumn<int>(
                name: "NoteId1",
                table: "NoteCategories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NoteCategories_NoteId1",
                table: "NoteCategories",
                column: "NoteId1");

            migrationBuilder.AddForeignKey(
                name: "FK_NoteCategories_Notes_NoteId1",
                table: "NoteCategories",
                column: "NoteId1",
                principalTable: "Notes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NoteCategories_Notes_NoteId1",
                table: "NoteCategories");

            migrationBuilder.DropIndex(
                name: "IX_NoteCategories_NoteId1",
                table: "NoteCategories");

            migrationBuilder.DropColumn(
                name: "NoteId1",
                table: "NoteCategories");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Notes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
