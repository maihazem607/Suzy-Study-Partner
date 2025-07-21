using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class StudySession
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string CreatorUserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsPublic { get; set; } = false;

        public int MaxParticipants { get; set; } = 10;

        public int CurrentParticipants { get; set; } = 1;

        [Required]
        public TimerType TimerType { get; set; } = TimerType.Pomodoro;

        public int StudyDuration { get; set; } = 25; // minutes

        public int BreakDuration { get; set; } = 5; // minutes

        public string? InviteCode { get; set; }

        // Navigation properties
        public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
        public ICollection<StudySessionParticipant> Participants { get; set; } = new List<StudySessionParticipant>();
        public ICollection<StudyTimerSession> TimerSessions { get; set; } = new List<StudyTimerSession>();
    }

    public enum TimerType
    {
        Pomodoro,    // 25 min study, 5 min break
        Flowmodoro,  // Study until you feel like taking a break
        Custom       // User defined times
    }

    public class StudySessionParticipant
    {
        public int Id { get; set; }

        public int StudySessionId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LeftAt { get; set; }

        public bool IsHost { get; set; } = false;

        public int TotalStudyTimeMinutes { get; set; } = 0;

        public DateTime? LastActivityAt { get; set; }

        // Navigation properties
        public StudySession StudySession { get; set; } = null!;
    }
}
