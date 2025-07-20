using System.Collections.Generic;

namespace Suzy.Models
{
    public class QuizQuestion
    {
        public string QuestionText { get; set; }
        public List<string> Options { get; set; }
        public string CorrectAnswer { get; set; }
        
        // This will store the user's selected answer during the test
        public string UserAnswer { get; set; } 
    }
}   