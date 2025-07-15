using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Suzy.Services;
using System.ComponentModel.DataAnnotations;

namespace Suzy.Pages.Flashcards
{
    [IgnoreAntiforgeryToken]
    public class FlashindexModel : PageModel
    {
        private readonly GeminiService _geminiService;
        private readonly ILogger<FlashindexModel> _logger;

        public FlashindexModel(GeminiService geminiService, ILogger<FlashindexModel> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        [BindProperty]
        public string? TestInput { get; set; }

        public List<Flashcard> TestFlashcards { get; set; } = new();
        public string Message { get; set; } = string.Empty;

        public void OnGet()
        {
            Message = "Enter some text to test the flashcard generator.";
        }
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Debug all form keys and values
                _logger.LogInformation("=== FORM SUBMISSION DEBUG ===");
                _logger.LogInformation("Form keys count: {Count}", Request.Form.Keys.Count);

                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogInformation("Form key: '{Key}' = '{Value}'", key, Request.Form[key]);
                }

                // Get the value directly from the form
                string? formInput = Request.Form["TestInput"];

                _logger.LogInformation("Direct form input: '{Input}'", formInput ?? "null");
                _logger.LogInformation("TestInput property: '{Property}'", TestInput ?? "null");

                // Use the direct form input
                string inputToUse = formInput ?? "";

                if (string.IsNullOrWhiteSpace(inputToUse))
                {
                    Message = "Please enter some text in the textarea.";
                    _logger.LogWarning("No input provided in form");
                    return Page();
                }

                _logger.LogInformation("Using input: '{Input}', calling Gemini service...", inputToUse);
                TestFlashcards = await _geminiService.GenerateFlashcardsAsync(inputToUse);
                Message = $"Generated {TestFlashcards.Count} test flashcards successfully!";

                _logger.LogInformation("Test completed successfully. Generated {Count} flashcards", TestFlashcards.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test page");
                Message = $"Test failed: {ex.Message}";
                TestFlashcards = new List<Flashcard>
                {
                    new Flashcard { Front = "Error Test", Back = $"Exception: {ex.Message}" }
                };
            }

            return Page();
        }
    }
}
