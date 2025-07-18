using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudySessionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "StudySessions",
                newName: "CreatorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_StudySessions_UserId",
                table: "StudySessions",
                newName: "IX_StudySessions_CreatorUserId");

            migrationBuilder.AddColumn<int>(
                name: "BreakDuration",
                table: "StudySessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentParticipants",
                table: "StudySessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "StudySessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "StudySessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxParticipants",
                table: "StudySessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StudyDuration",
                table: "StudySessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimerType",
                table: "StudySessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StudySessionParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudySessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsHost = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessionParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudySessionParticipants_StudySessions_StudySessionId",
                        column: x => x.StudySessionId,
                        principalTable: "StudySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudySessionParticipants");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_InviteCode",
                table: "StudySessions");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_IsPublic",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "BreakDuration",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "CurrentParticipants",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "MaxParticipants",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "StudyDuration",
                table: "StudySessions");

            migrationBuilder.DropColumn(
                name: "TimerType",
                table: "StudySessions");

            migrationBuilder.RenameColumn(
                name: "CreatorUserId",
                table: "StudySessions",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_StudySessions_CreatorUserId",
                table: "StudySessions",
                newName: "IX_StudySessions_UserId");
        }
    }
}
