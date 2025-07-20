using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    /// <summary>
    /// Represents an individual timer session within a study session
    /// Tracks each time a user starts and stops the timer
    /// </summary>
    public class StudyTimerSession
    {
        public int Id { get; set; }

        public int StudySessionId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Calculated duration in minutes when EndTime is set
        /// </summary>
        public int DurationMinutes { get; set; } = 0;

        /// <summary>
        /// Type of timer session: Study or Break
        /// </summary>
        public TimerSessionType SessionType { get; set; } = TimerSessionType.Study;

        /// <summary>
        /// Whether this timer session completed successfully or was interrupted
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Additional notes about the session (e.g., "User paused", "Timer completed naturally")
        /// </summary>
        public string? Notes { get; set; }

        // Navigation properties
        public StudySession StudySession { get; set; } = null!;
    }

    public enum TimerSessionType
    {
        Study,
        Break
    }
}
