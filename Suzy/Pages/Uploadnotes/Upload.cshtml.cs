using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Suzy.Data;
using Suzy.Models;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Suzy.Pages.Uploadnotes
{
    [Authorize]
    public class UploadModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UploadModel(IWebHostEnvironment environment, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _environment = environment;
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please enter a title.")]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please select a file.")]
        public IFormFile? Upload { get; set; }

        public List<Note> UserNotes { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            UserNotes = _context.Notes.Where(n => n.UserId == user.Id).ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || Upload == null || Upload.Length == 0)
            {
                TempData["Error"] = "Please provide a title and a valid file.";
                return await OnGetAsync();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(Upload.FileName)}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "uploads");

                Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Upload.CopyToAsync(stream);
                }

                var note = new Note
                {
                    Title = Title,
                    FilePath = $"/uploads/{fileName}",
                    UserId = user.Id
                };

                _context.Notes.Add(note);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Note uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Upload failed: {ex.Message}";
            }

            return RedirectToPage(); // triggers OnGetAsync again
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                TempData["Error"] = "Note not found.";
                return RedirectToPage();
            }

            var user = await _userManager.GetUserAsync(User);
            if (note.UserId != user.Id)
            {
                TempData["Error"] = "Unauthorized.";
                return RedirectToPage();
            }

            // Delete file
            var fullPath = Path.Combine(_environment.WebRootPath ?? "wwwroot", note.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Note deleted.";
            return RedirectToPage();
        }
    }
}
