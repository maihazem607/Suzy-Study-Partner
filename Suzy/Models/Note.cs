using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Suzy.Models
{
    public class Note
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string FilePath { get; set; }
        public string StoredFilePath { get; set; }
        public string UserId { get; set; }

        // 🔥 ADD THIS PROPERTY
        public ICollection<NoteCategory> NoteCategories { get; set; } = new List<NoteCategory>();
    }
}
