using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyTimerSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyTimerSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudySessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyTimerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyTimerSessions_StudySessions_StudySessionId",
                        column: x => x.StudySessionId,
                        principalTable: "StudySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyTimerSessions_StudySessionId",
                table: "StudyTimerSessions",
                column: "StudySessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyTimerSessions");
        }
    }
}
