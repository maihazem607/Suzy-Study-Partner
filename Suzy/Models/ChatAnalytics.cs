using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class ChatConversation
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public ChatPathType PathType { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public bool IsCompleted { get; set; } = false;

        public int CurrentStep { get; set; } = 1;

        // Navigation properties
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public bool IsFromUser { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? DataContext { get; set; } // JSON string of user data used for this message

        // Navigation properties
        public ChatConversation Conversation { get; set; } = null!;
    }

    public class StudyAnalytics
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public int TotalStudyMinutes { get; set; }

        public int TotalBreakMinutes { get; set; }

        public int CompletedTodos { get; set; }

        public int TotalTodos { get; set; }

        public int FlashcardsReviewed { get; set; }

        public double? MockExamScore { get; set; }

        public int FocusInterruptions { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WeeklySummary
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime WeekStartDate { get; set; }

        public int TotalStudyMinutes { get; set; }

        public int TotalBreakMinutes { get; set; }

        public double AverageStudyTimePerDay { get; set; }

        public int CompletedTodos { get; set; }

        public int TotalTodos { get; set; }

        public double ProductivityScore { get; set; }

        public int FlashcardsReviewed { get; set; }

        public double? AverageMockExamScore { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ChatPathType
    {
        StudyTimeAnalysis = 1,
        FocusAndPauses = 2,
        FlashcardProgress = 3,
        TodoProductivity = 4,
        WeeklySummary = 5,
        MockExamReview = 6
    }

    public class ChatPath
    {
        public ChatPathType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public List<string> Questions { get; set; } = new List<string>();
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
