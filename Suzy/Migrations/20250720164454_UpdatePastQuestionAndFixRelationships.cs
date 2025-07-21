using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePastQuestionAndFixRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PastPapers_Categories_CategoryId",
                table: "PastPapers");

            migrationBuilder.DropForeignKey(
                name: "FK_PastPapers_Notes_NoteId",
                table: "PastPapers");

            migrationBuilder.AlterColumn<int>(
                name: "NoteId",
                table: "PastPapers",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "PastPapers",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.CreateIndex(
                name: "IX_PastPapers_UserId",
                table: "PastPapers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PastPapers_AspNetUsers_UserId",
                table: "PastPapers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PastPapers_Categories_CategoryId",
                table: "PastPapers",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PastPapers_Notes_NoteId",
                table: "PastPapers",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PastPapers_AspNetUsers_UserId",
                table: "PastPapers");

            migrationBuilder.DropForeignKey(
                name: "FK_PastPapers_Categories_CategoryId",
                table: "PastPapers");

            migrationBuilder.DropForeignKey(
                name: "FK_PastPapers_Notes_NoteId",
                table: "PastPapers");

            migrationBuilder.DropIndex(
                name: "IX_PastPapers_UserId",
                table: "PastPapers");

            migrationBuilder.AlterColumn<int>(
                name: "NoteId",
                table: "PastPapers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "PastPapers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PastPapers_Categories_CategoryId",
                table: "PastPapers",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PastPapers_Notes_NoteId",
                table: "PastPapers",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
