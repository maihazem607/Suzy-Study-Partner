using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Suzy.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeStudySessionParticipants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create a new normalized StudySessionParticipants table without the redundant TotalStudyTimeMinutes column
            migrationBuilder.Sql(@"
                CREATE TABLE StudySessionParticipants_New (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    StudySessionId INTEGER NOT NULL,
                    UserId TEXT NOT NULL,
                    UserName TEXT NOT NULL,
                    JoinedAt TEXT NOT NULL,
                    IsHost INTEGER NOT NULL,
                    LastActivityAt TEXT,
                    FOREIGN KEY (StudySessionId) REFERENCES StudySessions(Id),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
                );
            ");

            // Step 2: Copy existing participant data (excluding the redundant TotalStudyTimeMinutes column)
            migrationBuilder.Sql(@"
                INSERT INTO StudySessionParticipants_New (Id, StudySessionId, UserId, UserName, JoinedAt, IsHost, LastActivityAt)
                SELECT Id, StudySessionId, UserId, UserName, JoinedAt, IsHost, LastActivityAt
                FROM StudySessionParticipants;
            ");

            // Step 3: Drop the old table
            migrationBuilder.Sql("DROP TABLE StudySessionParticipants;");

            // Step 4: Rename the new table
            migrationBuilder.Sql("ALTER TABLE StudySessionParticipants_New RENAME TO StudySessionParticipants;");

            // Step 5: Create a VIEW to dynamically calculate TotalStudyMinutes
            migrationBuilder.Sql(@"
                CREATE VIEW ParticipantStudyTime AS
                SELECT 
                    ssp.StudySessionId,
                    ssp.UserId,
                    COALESCE(SUM(sts.DurationMinutes), 0) AS TotalStudyMinutes
                FROM StudySessionParticipants ssp
                LEFT JOIN StudyTimerSessions sts
                    ON ssp.StudySessionId = sts.StudySessionId
                    AND ssp.UserId = sts.UserId
                    AND sts.SessionType = 0  -- 0 = Study (TimerSessionType.Study)
                GROUP BY ssp.StudySessionId, ssp.UserId;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the view
            migrationBuilder.Sql("DROP VIEW IF EXISTS ParticipantStudyTime;");

            // Step 2: Create the old table structure with TotalStudyTimeMinutes
            migrationBuilder.Sql(@"
                CREATE TABLE StudySessionParticipants_Old (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    StudySessionId INTEGER NOT NULL,
                    UserId TEXT NOT NULL,
                    UserName TEXT NOT NULL,
                    JoinedAt TEXT NOT NULL,
                    IsHost INTEGER NOT NULL,
                    TotalStudyTimeMinutes INTEGER NOT NULL DEFAULT 0,
                    LastActivityAt TEXT,
                    FOREIGN KEY (StudySessionId) REFERENCES StudySessions(Id),
                    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id)
                );
            ");

            // Step 3: Copy data back and calculate TotalStudyTimeMinutes
            migrationBuilder.Sql(@"
                INSERT INTO StudySessionParticipants_Old (Id, StudySessionId, UserId, UserName, JoinedAt, IsHost, TotalStudyTimeMinutes, LastActivityAt)
                SELECT 
                    ssp.Id, 
                    ssp.StudySessionId, 
                    ssp.UserId, 
                    ssp.UserName, 
                    ssp.JoinedAt, 
                    ssp.IsHost,
                    COALESCE(SUM(sts.DurationMinutes), 0) AS TotalStudyTimeMinutes,
                    ssp.LastActivityAt
                FROM StudySessionParticipants ssp
                LEFT JOIN StudyTimerSessions sts
                    ON ssp.StudySessionId = sts.StudySessionId
                    AND ssp.UserId = sts.UserId
                    AND sts.SessionType = 0  -- 0 = Study (TimerSessionType.Study)
                GROUP BY ssp.Id, ssp.StudySessionId, ssp.UserId, ssp.UserName, ssp.JoinedAt, ssp.IsHost, ssp.LastActivityAt;
            ");

            // Step 4: Drop current table and rename old one back
            migrationBuilder.Sql("DROP TABLE StudySessionParticipants;");
            migrationBuilder.Sql("ALTER TABLE StudySessionParticipants_Old RENAME TO StudySessionParticipants;");
        }
    }
}
