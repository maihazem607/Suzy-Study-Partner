using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class TodoItem
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Task { get; set; } = string.Empty;
        
        public bool IsCompleted { get; set; } = false;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public int? StudySessionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        public int Order { get; set; } = 0;
    }
}
