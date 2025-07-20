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

            // Fetch all test results for the current user, including the source documents.
            // Order them by the most recent test first.
            TestHistory = await _context.MockTestResults
                .Where(r => r.UserId == user.Id)
                .Include(r => r.SourceDocuments) 
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();

            return Page();
        }
    }
}