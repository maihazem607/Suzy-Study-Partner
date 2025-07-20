namespace Suzy.Models
{
    /// <summary>
    /// View model representing dynamically calculated participant study time
    /// This replaces the redundant TotalStudyTimeMinutes column in StudySessionParticipant
    /// </summary>
    public class ParticipantStudyTime
    {
        public int StudySessionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int TotalStudyMinutes { get; set; }

        // Navigation properties
        public StudySession StudySession { get; set; } = null!;
        public StudySessionParticipant Participant { get; set; } = null!;
    }
}
