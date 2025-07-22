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

namespace Suzy.Pages.Progress
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

        public List<MockTestResult> TestHistory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            TestHistory = await _context.MockTestResults
                .Where(r => r.UserId == user.Id)
                .Include(r => r.SourceDocuments) 
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();

            return Page();
        }

        // ✅ NEW: Handler to delete a specific test result
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Find the test result by its ID
            var testResult = await _context.MockTestResults.FindAsync(id);

            // Security check: ensure the result exists and belongs to the current user
            if (testResult == null || testResult.UserId != user.Id)
            {
                TempData["Error"] = "Test result not found or you are not authorized to delete it.";
                return RedirectToPage();
            }

            try
            {
                _context.MockTestResults.Remove(testResult);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Test result deleted successfully.";
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"Error deleting test result: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}