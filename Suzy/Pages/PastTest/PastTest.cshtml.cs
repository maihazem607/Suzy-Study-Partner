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
using Microsoft.AspNetCore.Hosting;
using System.Text;

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

        public List<PastTestPaper> UploadedPapers { get; set; } = new();
        
        // ✅ NEW: Property to hold the generated questions for display
        [BindProperty]
        public List<string> GeneratedQuestions { get; set; } = new();

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
        
        // No changes to OnPostAsync or OnPostDeleteAsync...
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
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
                string fileContent = "";
                try
                {
                    using (var reader = new StreamReader(UploadFile.OpenReadStream()))
                    {
                        fileContent = await reader.ReadToEndAsync();
                    }
                }
                catch
                {
                    fileContent = "Could not read content. File may be binary (e.g., PDF, image).";
                }
                var paper = new PastTestPaper
                {
                    Title = UploadTitle,
                    FilePath = $"/uploads/past_tests/{fileName}",
                    UserId = user.Id,
                    Content = fileContent
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
        // --- End of unchanged methods ---

        // ✅ NEW: Handler to generate questions
        public async Task<IActionResult> OnPostGenerateAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var paper = await _context.PastTestPapers.FindAsync(id);
            if (paper == null || paper.UserId != user.Id)
            {
                TempData["Error"] = "Could not find the requested document.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(paper.Content) || paper.Content.StartsWith("Could not read content"))
            {
                TempData["Error"] = "No readable text content found in this document to generate questions from.";
            }
            else
            {
                // This is where the magic happens!
                // We pass the stored text to a helper function.
                GeneratedQuestions = GenerateQuestionsFromText(paper.Content);
                TempData["Message"] = $"Successfully generated {GeneratedQuestions.Count} questions.";
            }

            // We must reload the list of papers to display the page correctly again.
            await OnGetAsync();
            return Page();
        }

        /// <summary>
        /// This is a placeholder for a real AI question generation service.
        /// It uses a simple algorithm to create sample questions.
        /// </summary>
        private List<string> GenerateQuestionsFromText(string text)
        {
            var questions = new List<string>();
            var sentences = text.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            // Simple logic: Turn the first 3-4 long sentences into questions.
            foreach (var sentence in sentences.Where(s => s.Length > 50).Take(3))
            {
                questions.Add($"Based on the text, what is the significance of the following: \"{sentence.Trim()}\"?");
            }

            // Add a general question
            if (text.Contains("code"))
            {
                questions.Add("Explain the role of debugging as described in the document.");
            }
            if (questions.Count == 0)
            {
                questions.Add("What is the main topic of the document?");
            }

            return questions;
        }
    }
}