using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_StudySessions_StudySessionId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_UserId",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_CreatorUserId",
                table: "StudySessions");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_InviteCode",
                table: "StudySessions");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_IsPublic",
                table: "StudySessions");

            migrationBuilder.DropIndex(
                name: "IX_StudySessionParticipants_StudySessionId_UserId",
                table: "StudySessionParticipants");

            migrationBuilder.DropIndex(
                name: "IX_StudySessionParticipants_UserId",
                table: "StudySessionParticipants");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "Notes",
                newName: "StoredFilePath");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TodoItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "StudySessions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "StudySessionParticipants",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "Notes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NoteCategories",
                columns: table => new
                {
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteCategories", x => new { x.NoteId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_NoteCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NoteCategories_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudySessionParticipants_StudySessionId",
                table: "StudySessionParticipants",
                column: "StudySessionId");

            migrationBuilder.CreateIndex(
                name: "IX_NoteCategories_CategoryId",
                table: "NoteCategories",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_StudySessions_StudySessionId",
                table: "TodoItems",
                column: "StudySessionId",
                principalTable: "StudySessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoItems_StudySessions_StudySessionId",
                table: "TodoItems");

            migrationBuilder.DropTable(
                name: "NoteCategories");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_StudySessionParticipants_StudySessionId",
                table: "StudySessionParticipants");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "Notes");

            migrationBuilder.RenameColumn(
                name: "StoredFilePath",
                table: "Notes",
                newName: "FileName");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TodoItems",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "StudySessions",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "StudySessionParticipants",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_UserId",
                table: "TodoItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_CreatorUserId",
                table: "StudySessions",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_InviteCode",
                table: "StudySessions",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_IsPublic",
                table: "StudySessions",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessionParticipants_StudySessionId_UserId",
                table: "StudySessionParticipants",
                columns: new[] { "StudySessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudySessionParticipants_UserId",
                table: "StudySessionParticipants",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoItems_StudySessions_StudySessionId",
                table: "TodoItems",
                column: "StudySessionId",
                principalTable: "StudySessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
