using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceDocumentsToMockTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MockTestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalQuestions = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockTestResults_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockTestQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MockTestResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionText = table.Column<string>(type: "TEXT", nullable: false),
                    OptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "TEXT", nullable: false),
                    UserAnswer = table.Column<string>(type: "TEXT", nullable: false),
                    IsCorrect = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockTestQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockTestQuestions_MockTestResults_MockTestResultId",
                        column: x => x.MockTestResultId,
                        principalTable: "MockTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MockTestSourceDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MockTestResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceDocumentName = table.Column<string>(type: "TEXT", nullable: false),
                    SourceDocumentType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MockTestSourceDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MockTestSourceDocuments_MockTestResults_MockTestResultId",
                        column: x => x.MockTestResultId,
                        principalTable: "MockTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MockTestQuestions_MockTestResultId",
                table: "MockTestQuestions",
                column: "MockTestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_MockTestResults_UserId",
                table: "MockTestResults",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MockTestSourceDocuments_MockTestResultId",
                table: "MockTestSourceDocuments",
                column: "MockTestResultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MockTestQuestions");

            migrationBuilder.DropTable(
                name: "MockTestSourceDocuments");

            migrationBuilder.DropTable(
                name: "MockTestResults");
        }
    }
}
