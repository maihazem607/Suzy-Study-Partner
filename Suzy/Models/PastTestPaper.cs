using Microsoft.AspNetCore.Identity;
using System.Collections.Generic; // Added for ICollection
using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class PastTestPaper
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string FilePath { get; set; }

        // ✅ ADD THIS PROPERTY TO STORE THE FILE'S CONTENT
        public string Content { get; set; }

        [Required]
        public string UserId { get; set; }

        // Navigation property to the user
        public IdentityUser User { get; set; }
        public ICollection<PastTestPaperCategory> PastTestPaperCategories { get; set; }
    }
}