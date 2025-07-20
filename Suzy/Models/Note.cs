using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class Note
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string StoredFilePath { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = ""; // ✅ Add this to store file contents

        public ICollection<NoteCategory> NoteCategories { get; set; } = new List<NoteCategory>();
    }
}
