using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Suzy.Pages.Settings
{
    [Authorize]
    public class SettingsModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SettingsModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty]
        public IFormFile ApiKeyFile { get; set; }

        public bool IsApiKeyFilePresent { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string OldPassword { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm new password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            Username = user.UserName;
            Input = new InputModel { Email = user.Email };

            // Check for the standardized API key file name
            var apiKeyPath = Path.Combine(_env.ContentRootPath, "suzy-gemini-key.json");
            IsApiKeyFilePresent = System.IO.File.Exists(apiKeyPath);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateApiKeyAsync()
        {
            if (ApiKeyFile == null || ApiKeyFile.Length == 0)
            {
                StatusMessage = "Error: Please select a file to upload.";
                return RedirectToPage();
            }

            if (Path.GetExtension(ApiKeyFile.FileName).ToLower() != ".json")
            {
                StatusMessage = "Error: Invalid file type. Please upload a .json file.";
                return RedirectToPage();
            }

            // MODIFIED: Standardized the target filename for the API key.
            var targetFileName = "suzy-gemini-key.json";
            var filePath = Path.Combine(_env.ContentRootPath, targetFileName);

            try
            {
                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ApiKeyFile.CopyToAsync(stream);
                }
                StatusMessage = "API Key file updated successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: Failed to save the file. {ex.Message}";
            }

            return RedirectToPage();
        }
        
        // ... The rest of your methods (OnPostUpdateProfileAsync, etc.) remain unchanged ...
        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload necessary properties
                return Page();
            }

            if (Input.Email != user.Email)
            {
                user.Email = Input.Email;
                user.UserName = Input.Email;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    StatusMessage = "Error: Unexpected error when trying to set email.";
                    return RedirectToPage();
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(Input.OldPassword) || string.IsNullOrEmpty(Input.NewPassword))
            {
                ModelState.AddModelError(string.Empty, "Password fields cannot be empty.");
                await OnGetAsync();
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await OnGetAsync();
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your password has been changed.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostExportDataAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var data = new
            {
                notes = await _context.Notes.Where(n => n.UserId == user.Id).ToListAsync(),
                categories = await _context.Categories.Where(c => c.UserId == user.Id).ToListAsync(),
                pastTestPapers = await _context.PastTestPapers.Where(p => p.UserId == user.Id).ToListAsync(),
                mockTestResults = await _context.MockTestResults
                    .Where(r => r.UserId == user.Id)
                    .Include(r => r.Questions)
                    .Include(r => r.SourceDocuments)
                    .ToListAsync()
            };

            var options = new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
            var json = JsonSerializer.Serialize(data, options);
            return File(Encoding.UTF8.GetBytes(json), "application/json", $"suzy_export_{DateTime.Now:yyyyMMdd}.json");
        }

        public async Task<IActionResult> OnPostDeleteAccountAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            
            _context.Notes.RemoveRange(_context.Notes.Where(n => n.UserId == user.Id));
            _context.Categories.RemoveRange(_context.Categories.Where(c => c.UserId == user.Id));
            _context.PastTestPapers.RemoveRange(_context.PastTestPapers.Where(p => p.UserId == user.Id));
            _context.MockTestResults.RemoveRange(_context.MockTestResults.Where(r => r.UserId == user.Id));
            await _context.SaveChangesAsync();
            
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Unexpected error deleting user.");
            }

            await _signInManager.SignOutAsync();
            return Redirect("~/");
        }
        
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Index"); 
        }
    }
}