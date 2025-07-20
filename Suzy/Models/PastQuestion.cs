// D:\CLG\9th sem\lab\kahrabha\maigit\SmartPrep\Suzy\Models\PastQuestion.cs
// This file is correct.

namespace Suzy.Models
{
    public class PastQuestion
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string FilePath { get; set; } = "";
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int NoteId { get; set; }
        public Note? Note { get; set; }
        public string UserId { get; set; } = "";
    }
}