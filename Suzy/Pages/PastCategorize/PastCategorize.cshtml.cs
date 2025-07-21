using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Suzy.Pages.PastCategorize
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public string NewCategoryName { get; set; } = string.Empty;
        public List<Category> Categories { get; set; } = new();
        public List<PastTestPaper> TestPapers { get; set; } = new();
        public Dictionary<int, List<int>> PaperCategories { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            Categories = await _context.Categories.Where(c => c.UserId == user.Id).ToListAsync();
            TestPapers = await _context.PastTestPapers.Where(p => p.UserId == user.Id).ToListAsync();

            PaperCategories = await _context.PastTestPaperCategories
                .Where(pc => pc.PastTestPaper.UserId == user.Id)
                .GroupBy(pc => pc.PastTestPaperId)
                .ToDictionaryAsync(g => g.Key, g => g.Select(pc => pc.CategoryId).ToList());

            return Page();
        }

        public async Task<IActionResult> OnPostAddCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
            {
                TempData["Error"] = "Category name cannot be empty.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
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
            var category = await _context.Categories.FindAsync(id);

            if (category == null || category.UserId != user.Id)
            {
                TempData["Error"] = "Category not found or you are not authorized.";
                return RedirectToPage();
            }

            var mappings = _context.PastTestPaperCategories.Where(pc => pc.CategoryId == id);
            _context.PastTestPaperCategories.RemoveRange(mappings);
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Category deleted successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSaveCategorizationAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Clear all existing mappings for this user
            var existingMappings = _context.PastTestPaperCategories
                .Where(pc => pc.PastTestPaper.UserId == user.Id);
            _context.PastTestPaperCategories.RemoveRange(existingMappings);

            // Add new mappings from the form
            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("Selections[")))
            {
                // Key is like "Selections[paperId]"
                var paperIdStr = key.Split('[', ']')[1];
                if (int.TryParse(paperIdStr, out int paperId))
                {
                    foreach (var categoryIdStr in Request.Form[key])
                    {
                        if (int.TryParse(categoryIdStr, out int categoryId))
                        {
                            _context.PastTestPaperCategories.Add(new PastTestPaperCategory
                            {
                                PastTestPaperId = paperId,
                                CategoryId = categoryId
                            });
                        }
                    }
                }
            }
            
            await _context.SaveChangesAsync();
            TempData["Message"] = "Categorization saved successfully.";
            return RedirectToPage();
        }
    }
}