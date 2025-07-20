namespace Suzy.Models
{
    // This new model creates the many-to-many relationship
    // between your PastTestPaper and Category models.
    public class PastTestPaperCategory
    {
        public int PastTestPaperId { get; set; }
        public PastTestPaper PastTestPaper { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}