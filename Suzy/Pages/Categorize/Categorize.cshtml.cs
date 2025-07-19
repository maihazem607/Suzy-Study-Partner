using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Suzy.Data;
using Suzy.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Suzy.Pages.Categorize
{
    [Authorize]
    public class CategorizeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CategorizeModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public string NewCategoryName { get; set; } = string.Empty;

        public List<Note> Notes { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public Dictionary<int, List<int>> NoteCategories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Handles null user

            Notes = _context.Notes.Where(n => n.UserId == user.Id).ToList();
            Categories = _context.Categories.Where(c => c.UserId == user.Id).ToList();

            NoteCategories = _context.NoteCategories
                .Where(nc => nc.Note.UserId == user.Id)
                .GroupBy(nc => nc.NoteId)
                .ToDictionary(g => g.Key, g => g.Select(nc => nc.CategoryId).ToList());

            return Page();
        }

        public async Task<IActionResult> OnPostAddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                TempData["Error"] = "Category name cannot be empty.";
                return await OnGetAsync();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var category = new Category
            {
                Name = NewCategoryName.Trim(),
                UserId = user.Id
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Category added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteCategoryAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var category = await _context.Categories.FindAsync(id);
            if (category == null || category.UserId != user.Id)
            {
                TempData["Error"] = "Category not found or unauthorized.";
                return RedirectToPage();
            }

            var mappings = _context.NoteCategories.Where(nc => nc.CategoryId == id).ToList();
            _context.NoteCategories.RemoveRange(mappings);

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Category deleted.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveCategorizationAsync(Dictionary<int, int> Selections)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var allNotes = _context.Notes.Where(n => n.UserId == user.Id).ToList();
            var allCategories = _context.Categories.Where(c => c.UserId == user.Id).ToList();

            var existingMappings = _context.NoteCategories
                .Where(nc => nc.Note.UserId == user.Id)
                .ToList();

            _context.NoteCategories.RemoveRange(existingMappings);

            // Safe parsing and re-adding selections
            foreach (var kvp in Request.Form.Where(k => k.Key.StartsWith("Selections[")))
            {
                var keyPart = kvp.Key.Split('[', ']');
                if (keyPart.Length >= 2 &&
                    int.TryParse(keyPart[1], out int noteId) &&
                    int.TryParse(kvp.Value, out int categoryId))
                {
                    _context.NoteCategories.Add(new NoteCategory
                    {
                        NoteId = noteId,
                        CategoryId = categoryId
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Categorization saved.";
            return RedirectToPage();
        }
    }
}
