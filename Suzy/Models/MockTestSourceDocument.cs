using System.ComponentModel.DataAnnotations;

namespace Suzy.Models
{
    public class MockTestSourceDocument
    {
        public int Id { get; set; }

        public int MockTestResultId { get; set; }
        public MockTestResult MockTestResult { get; set; }

        [Required]
        public string SourceDocumentName { get; set; } // e.g., "Chapter 5 Notes"

        [Required]
        public string SourceDocumentType { get; set; } // e.g., "Note" or "Past Test"
    }
}