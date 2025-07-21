using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace Suzy.Pages.PastTest
{
    [Authorize]
    public class PastTestModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public PastTestModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please enter a title.")]
        [StringLength(100, MinimumLength = 3)]
        public string UploadTitle { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Please select a file to upload.")]
        public IFormFile UploadFile { get; set; } = null!;

        // This property MUST exist in your file for the view to work.
        public List<PastTestPaper> UploadedPapers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            UploadedPapers = await _context.PastTestPapers
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "past_tests");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(UploadFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                await using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadFile.CopyToAsync(stream);
                }

                var paper = new PastTestPaper
                {
                    Title = UploadTitle,
                    FilePath = $"/uploads/past_tests/{fileName}",
                    UserId = user.Id
                };

                _context.PastTestPapers.Add(paper);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Test paper uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Upload failed: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var paper = await _context.PastTestPapers.FindAsync(id);
            if (paper == null || paper.UserId != user.Id)
            {
                TempData["Error"] = "Item not found or you are not authorized to delete it.";
                return RedirectToPage();
            }

            try
            {
                if (!string.IsNullOrEmpty(paper.FilePath))
                {
                    var fullPath = Path.Combine(_env.WebRootPath, paper.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                _context.PastTestPapers.Remove(paper);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Test paper deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Deletion failed: {ex.Message}";
            }
            
            return RedirectToPage();
        }
    }
}