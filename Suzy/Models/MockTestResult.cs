using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class MockTestResult
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public string Subject { get; set; }
        public DateTime Timestamp { get; set; }
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        
        public ICollection<MockTestQuestion> Questions { get; set; }

        // --- ADD THIS LINE ---
        public ICollection<MockTestSourceDocument> SourceDocuments { get; set; }
    }
}