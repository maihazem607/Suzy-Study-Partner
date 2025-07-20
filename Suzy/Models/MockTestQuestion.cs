using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Suzy.Models
{
    public class MockTestQuestion
    {
        public int Id { get; set; }

        public int MockTestResultId { get; set; }
        public MockTestResult MockTestResult { get; set; }

        [Required]
        public string QuestionText { get; set; }

        // We will store the list of options as a single JSON string
        public string OptionsJson { get; set; }

        public string CorrectAnswer { get; set; }
        public string UserAnswer { get; set; }
        public bool IsCorrect { get; set; }

        [NotMapped]
        public List<string> Options { get; set; }
    }
}