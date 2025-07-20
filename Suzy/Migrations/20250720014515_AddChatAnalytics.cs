using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class AddChatAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PathType = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentStep = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyAnalytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalStudyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBreakMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    FlashcardsReviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    MockExamScore = table.Column<double>(type: "REAL", nullable: true),
                    FocusInterruptions = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyAnalytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeeklySummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalStudyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBreakMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageStudyTimePerDay = table.Column<double>(type: "REAL", nullable: false),
                    CompletedTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTodos = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductivityScore = table.Column<double>(type: "REAL", nullable: false),
                    FlashcardsReviewed = table.Column<int>(type: "INTEGER", nullable: false),
                    AverageMockExamScore = table.Column<double>(type: "REAL", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklySummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConversationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    IsFromUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DataContext = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId",
                table: "ChatMessages",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "StudyAnalytics");

            migrationBuilder.DropTable(
                name: "WeeklySummaries");

            migrationBuilder.DropTable(
                name: "ChatConversations");
        }
    }
}
