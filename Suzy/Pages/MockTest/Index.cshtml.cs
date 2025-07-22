using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using Suzy.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using System.Text.Json;

namespace Suzy.Pages.MockTest
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly GeminiService _geminiService;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _geminiService = new GeminiService();
        }

        // ... (No changes to properties or OnGetAsync) ...
        [BindProperty]
        public int SubjectId { get; set; }

        [BindProperty]
        public List<string> SelectedContent { get; set; }

        public List<SelectListItem> SubjectOptions { get; set; }

        public List<QuizQuestion> QuizQuestions { get; set; }

        public int? Score { get; set; }
        public List<QuizQuestion> TestResults { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            SubjectOptions = await _context.Categories
                .Where(c => c.UserId == user.Id)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

            if (TempData.ContainsKey("TestResults"))
            {
                TestResults = JsonSerializer.Deserialize<List<QuizQuestion>>(TempData["TestResults"].ToString());
                Score = (int)TempData["Score"];
                TempData.Keep("TestResults");
                TempData.Keep("Score");
            }
        }

        public async Task<JsonResult> OnGetContentsForSubject(int subjectId)
        {
            var user = await _userManager.GetUserAsync(User);
            var notes = await _context.NoteCategories
                .Where(nc => nc.CategoryId == subjectId && nc.Note.UserId == user.Id)
                .Select(nc => new { Id = $"note_{nc.NoteId}", Text = $"Note: {nc.Note.Title}" })
                .ToListAsync();

            var pastTests = await _context.PastTestPaperCategories
                .Where(pc => pc.CategoryId == subjectId && pc.PastTestPaper.UserId == user.Id)
                .Select(pc => new { Id = $"pasttest_{pc.PastTestPaperId}", Text = $"Past Test: {pc.PastTestPaper.Title}" })
                .ToListAsync();

            return new JsonResult(notes.Concat(pastTests));
        }


        public async Task<JsonResult> OnPostGenerateTestAsync()
        {
            if (SelectedContent == null || !SelectedContent.Any())
            {
                return new JsonResult(new { success = false, message = "Please select at least one document." });
            }

            TempData["SourceContentIds"] = JsonSerializer.Serialize(SelectedContent);

            var combinedContent = new StringBuilder();
            foreach (var contentId in SelectedContent)
            {
                var parts = contentId.Split('_');
                var type = parts[0];
                var id = int.Parse(parts[1]);

                if (type == "note")
                {
                    var note = await _context.Notes.FindAsync(id);
                    if (note != null) combinedContent.AppendLine(note.Content);
                }
                else if (type == "pasttest")
                {
                    var test = await _context.PastTestPapers.FindAsync(id);
                    if (test != null)
                    {
                        var filePath = Path.Combine(_env.WebRootPath, test.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            // --- ✅ MODIFIED LOGIC STARTS HERE ---
                            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();

                            if (fileExtension == ".pdf")
                            {
                                // If it's a PDF, use the PDF reader
                                combinedContent.AppendLine(ReadPdfContent(filePath));
                            }
                            else if (fileExtension == ".txt")
                            {
                                // If it's a TXT file, use the new text reader
                                combinedContent.AppendLine(await ReadTextFileContentAsync(filePath));
                            }
                            else
                            {
                                // Handle other file types gracefully
                                combinedContent.AppendLine($"\n[Content from '{test.Title}' could not be read. Unsupported file type: {fileExtension}]\n");
                            }
                            // --- ✅ MODIFIED LOGIC ENDS HERE ---
                        }
                    }
                }
            }

            var prompt = $"Based on the following text, generate exactly 10 multiple-choice questions for a test. Each question must have four options. Format the entire output as a single, valid JSON array. Each object in the array should have three properties: \"QuestionText\" (string), \"Options\" (an array of 4 strings), and \"CorrectAnswer\" (a string that exactly matches one of the options). Do not include any text, markdown like ```json, or formatting outside of the JSON array. \n\n---TEXT---\n{combinedContent}";

            try
            {
                // ... (No changes to the Gemini service call and response parsing) ...
                var rawResponse = await _geminiService.GenerateContentWithRetryAsync(prompt);
                var root = JsonDocument.Parse(rawResponse).RootElement;
                var responseText = root
                    .GetProperty("candidates")[0].GetProperty("content")
                    .GetProperty("parts")[0].GetProperty("text").GetString() ?? "";

                responseText = responseText.Trim();
                if (responseText.StartsWith("```"))
                {
                    int firstNewline = responseText.IndexOf('\n');
                    int lastBackticks = responseText.LastIndexOf("```");
                    if (firstNewline != -1 && lastBackticks > firstNewline)
                    {
                        responseText = responseText.Substring(firstNewline + 1, lastBackticks - firstNewline - 1).Trim();
                    }
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var questions = JsonSerializer.Deserialize<List<QuizQuestion>>(responseText, options);

                if (questions == null || questions.Count == 0)
                {
                    throw new Exception("AI returned an empty or invalid list of questions.");
                }

                TempData["QuizAnswers"] = JsonSerializer.Serialize(questions);
                return new JsonResult(new { success = true, questions = questions });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Failed to generate quiz: {ex.Message}" });
            }
        }

        // ... (No changes to OnPostSubmitTest) ...
        public async Task<IActionResult> OnPostSubmitTest([FromForm] Dictionary<int, string> answers)
        {
            var answersJson = TempData["QuizAnswers"]?.ToString();
            var sourceContentIdsJson = TempData["SourceContentIds"]?.ToString();

            if (string.IsNullOrEmpty(answersJson) || string.IsNullOrEmpty(sourceContentIdsJson))
            {
                TempData["Error"] = "Quiz session expired. Please generate a new one.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            var generatedQuestions = JsonSerializer.Deserialize<List<QuizQuestion>>(answersJson);
            var sourceContentIds = JsonSerializer.Deserialize<List<string>>(sourceContentIdsJson);
            int score = 0;

            var result = new MockTestResult
            {
                UserId = user.Id,
                Timestamp = DateTime.Now,
                TotalQuestions = generatedQuestions.Count,
                Subject = "Mock Test",
                Questions = new List<MockTestQuestion>(),
                SourceDocuments = new List<MockTestSourceDocument>()
            };

            foreach (var contentId in sourceContentIds)
            {
                var parts = contentId.Split('_');
                var type = parts[0];
                var id = int.Parse(parts[1]);
                string title = "Unknown";
                string docType = "Unknown";

                if (type == "note")
                {
                    var note = await _context.Notes.FindAsync(id);
                    if (note != null) title = note.Title;
                    docType = "Note";
                }
                else if (type == "pasttest")
                {
                    var test = await _context.PastTestPapers.FindAsync(id);
                    if (test != null) title = test.Title;
                    docType = "Past Test";
                }

                result.SourceDocuments.Add(new MockTestSourceDocument
                {
                    SourceDocumentName = title,
                    SourceDocumentType = docType
                });
            }

            for (int i = 0; i < generatedQuestions.Count; i++)
            {
                var userAns = answers.ContainsKey(i) ? answers[i] : "Not Answered";
                var isCorrect = userAns == generatedQuestions[i].CorrectAnswer;
                if (isCorrect) score++;

                result.Questions.Add(new MockTestQuestion
                {
                    QuestionText = generatedQuestions[i].QuestionText,
                    OptionsJson = JsonSerializer.Serialize(generatedQuestions[i].Options),
                    CorrectAnswer = generatedQuestions[i].CorrectAnswer,
                    UserAnswer = userAns,
                    IsCorrect = isCorrect
                });
            }

            result.Score = score;

            _context.MockTestResults.Add(result);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Result", new { id = result.Id });
        }


        // ✅ NEW: Simple method to read plain text files
        private async Task<string> ReadTextFileContentAsync(string filePath)
        {
            return await System.IO.File.ReadAllTextAsync(filePath);
        }

        // This method is now only used for PDF files
        private string ReadPdfContent(string filePath)
        {
            var sb = new StringBuilder();
            using (var pdfReader = new PdfReader(filePath))
            using (var pdfDoc = new PdfDocument(pdfReader))
            {
                for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    sb.AppendLine(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page)));
                }
            }
            return sb.ToString();
        }
    }
}