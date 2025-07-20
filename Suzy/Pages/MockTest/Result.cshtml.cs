using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Suzy.Pages.MockTest
{
    [Authorize]
    public class ResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ResultModel(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public MockTestResult TestResult { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            TestResult = await _context.MockTestResults
                .Include(r => r.Questions)
                .Include(r => r.SourceDocuments)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (TestResult == null)
            {
                return NotFound();
            }

            foreach (var q in TestResult.Questions)
            {
                q.Options = JsonSerializer.Deserialize<List<string>>(q.OptionsJson);
            }

            return Page();
        }
    }
}