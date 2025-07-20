using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using System.ComponentModel.DataAnnotations;

namespace Suzy.Pages.Mock
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // --- Bind Properties with Validation ---
        [BindProperty]
        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int SelectedCategoryId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please select a note.")]
        [Display(Name = "Note")]
        public int SelectedNoteId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please enter a title.")]
        [StringLength(100, MinimumLength = 3)]
        public string UploadTitle { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile UploadFile { get; set; } = null!;

        // --- Properties for Dropdowns ---
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> NoteOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                // If validation fails, reload dropdowns to show the user's previous selections
                await LoadCategoriesAsync();
                await LoadNotesForCategoryAsync(user.Id, SelectedCategoryId);
                return Page();
            }

            // --- Save File ---
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "pastquestions");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid() + Path.GetExtension(UploadFile.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await UploadFile.CopyToAsync(stream);
            }

            // --- Save to Database ---
            var question = new PastQuestion
            {
                Title = UploadTitle,
                FilePath = "/uploads/pastquestions/" + fileName, // Web-accessible path
                CategoryId = SelectedCategoryId,
                NoteId = SelectedNoteId,
                UserId = user.Id
            };

            _context.PastPapers.Add(question); // Using the correct DbSet 'PastPapers'
            await _context.SaveChangesAsync();

            TempData["Message"] = "Past question uploaded successfully.";
            return RedirectToPage();
        }

        /// <summary>
        /// Handler called by JavaScript to dynamically load notes based on the selected category.
        /// </summary>
        public async Task<IActionResult> OnGetNotes(int categoryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new List<SelectListItem>());
            }

            var notes = await _context.NoteCategories
                .Where(nc => nc.CategoryId == categoryId && nc.Note.UserId == user.Id)
                .Select(nc => new SelectListItem
                {
                    Value = nc.NoteId.ToString(),
                    Text = nc.Note.Title
                })
                .ToListAsync();

            return new JsonResult(notes);
        }

        // --- Helper Methods to Load Data ---
        private async Task LoadCategoriesAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                CategoryOptions = await _context.Categories
                    .Where(c => c.UserId == user.Id)
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();
            }
        }
        
        // Helper to reload notes when validation fails on POST
        private async Task LoadNotesForCategoryAsync(string userId, int categoryId)
        {
             NoteOptions = await _context.NoteCategories
                .Where(nc => nc.CategoryId == categoryId && nc.Note.UserId == userId)
                .Select(nc => new SelectListItem { Value = nc.NoteId.ToString(), Text = nc.Note.Title })
                .ToListAsync();
        }
    }
}