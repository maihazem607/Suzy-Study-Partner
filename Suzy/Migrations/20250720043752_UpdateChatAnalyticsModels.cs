using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChatAnalyticsModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TotalStudyTimeMinutes",
                table: "StudySessionParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalStudyTimeMinutes",
                table: "StudySessionParticipants");
        }
    }
}
