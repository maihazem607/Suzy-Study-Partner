using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    // This is a new, self-contained model for your test papers.
    // It has NO connection to Category or Note.
    public class PastTestPaper
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string FilePath { get; set; }

        [Required]
        public string UserId { get; set; }

        // Navigation property to the user
        public IdentityUser User { get; set; }
        public ICollection<PastTestPaperCategory> PastTestPaperCategories { get; set; }
    }
}