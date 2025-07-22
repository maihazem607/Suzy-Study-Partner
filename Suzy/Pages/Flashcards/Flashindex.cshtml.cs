using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using Suzy.Services;
using System.Text.Json;

namespace Suzy.Pages.Flashcards
{
    public class FlashindexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly GeminiService _geminiService;
        private readonly UserManager<IdentityUser> _userManager;

        public FlashindexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _geminiService = new GeminiService(); // You can inject this via DI if desired
        }

        [BindProperty(SupportsGet = true)]
        public int SelectedCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int SelectedNoteId { get; set; }

        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> NoteOptions { get; set; } = new();

        public string LoadedContent { get; set; } = "";
        public List<Suzy.Models.Flashcard> Flashcards { get; set; } = new();

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await LoadCategoriesAsync(user.Id);

                if (SelectedCategoryId > 0)
                {
                    await LoadNotesAsync(user.Id);
                }
            }
        }

        public async Task<IActionResult> OnPostLoadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await LoadCategoriesAsync(user.Id);
            await LoadNotesAsync(user.Id);

            var note = await _context.Notes.FindAsync(SelectedNoteId);
            if (note == null)
            {
                TempData["Error"] = "Note not found.";
                return Page();
            }

            LoadedContent = note.Content ?? "(No content found)";
            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await LoadCategoriesAsync(user.Id);
            await LoadNotesAsync(user.Id);

            var note = await _context.Notes.FindAsync(SelectedNoteId);
            if (note == null)
            {
                TempData["Error"] = "Note not found.";
                return Page();
            }

            LoadedContent = note.Content ?? "(No content found)";

            try
            {
                var prompt = @"Generate exactly 5 flashcards based on the following note content.
Return the result strictly as JSON in the following format:
[
  { ""Question"": ""What is ...?"", ""Answer"": ""..."" },
  ...
]
Content:
" + LoadedContent;

                var jsonResponse = await _geminiService.GenerateContentAsync(prompt);

                var root = JsonDocument.Parse(jsonResponse).RootElement;
                var responseText = root
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "";

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

                if (!responseText.StartsWith("["))
                {
                    throw new Exception("Response did not contain valid JSON flashcards.\nContent: " + responseText);
                }

                Flashcards = JsonSerializer.Deserialize<List<Suzy.Models.Flashcard>>(responseText) ?? new();
                TempData["Message"] = "Flashcards generated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating flashcards: {ex.Message}";
            }

            return Page();
        }

        public async Task<JsonResult> OnGetNotesAsync(int categoryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return new JsonResult(new List<SelectListItem>());

            var notes = await _context.NoteCategories
                .Where(nc => nc.CategoryId == categoryId && nc.Note.UserId == user.Id)
                .Select(nc => nc.Note)
                .Distinct()
                .Select(n => new SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = n.Title
                })
                .ToListAsync();

            return new JsonResult(notes);
        }

        private async Task LoadCategoriesAsync(string userId)
        {
            CategoryOptions = await _context.Categories
                .Where(c => c.UserId == userId)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

        private async Task LoadNotesAsync(string userId)
        {
            NoteOptions = await _context.NoteCategories
                .Where(nc => nc.CategoryId == SelectedCategoryId && nc.Note.UserId == userId)
                .Select(nc => nc.Note)
                .Distinct()
                .Select(n => new SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = n.Title
                })
                .ToListAsync();
        }
    }
}