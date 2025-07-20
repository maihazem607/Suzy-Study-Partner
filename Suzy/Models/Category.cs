using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string UserId { get; set; }

        public ICollection<NoteCategory> NoteCategories { get; set; } = new List<NoteCategory>();
        public ICollection<PastTestPaperCategory> PastTestPaperCategories { get; set; }
    }
}
