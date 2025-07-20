using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using Suzy.Services;
using System.Security.Claims;

namespace Suzy.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ChatAnalyticsService _analyticsService;

        public TodoController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ChatAnalyticsService analyticsService)
        {
            _context = context;
            _userManager = userManager;
            _analyticsService = analyticsService;
        }

        [HttpGet("GetTodos")]
        public async Task<IActionResult> GetTodos(int? studySessionId = null)
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.TodoItems
                .Where(t => t.UserId == userId);

            if (studySessionId.HasValue)
            {
                query = query.Where(t => t.StudySessionId == studySessionId.Value);
            }
            else
            {
                // Get todos not associated with any study session
                query = query.Where(t => t.StudySessionId == null);
            }

            var todos = await query
                .OrderBy(t => t.Order)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();

            return Ok(todos);
        }

        [HttpPost("CreateTodo")]
        public async Task<IActionResult> CreateTodo([FromBody] CreateTodoRequest request)
        {
            var userId = _userManager.GetUserId(User);

            var todo = new TodoItem
            {
                Task = request.Task,
                UserId = userId!,
                StudySessionId = request.StudySessionId,
                Order = request.Order
            };

            _context.TodoItems.Add(todo);
            await _context.SaveChangesAsync();

            return Ok(todo);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, [FromBody] UpdateTodoRequest request)
        {
            var userId = _userManager.GetUserId(User);

            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(request.Task))
            {
                todo.Task = request.Task;
            }

            if (request.IsCompleted.HasValue)
            {
                todo.IsCompleted = request.IsCompleted.Value;
                todo.CompletedAt = request.IsCompleted.Value ? DateTime.UtcNow : null;
            }

            if (request.Order.HasValue)
            {
                todo.Order = request.Order.Value;
            }

            await _context.SaveChangesAsync();

            // Automatically update daily analytics when todo status changes
            if (request.IsCompleted.HasValue)
            {
                try
                {
                    await _analyticsService.UpdateDailyAnalyticsAsync(userId!);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the request
                    Console.WriteLine($"Error updating analytics: {ex.Message}");
                }
            }

            return Ok(todo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var userId = _userManager.GetUserId(User);

            var todo = await _context.TodoItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (todo == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todo);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class CreateTodoRequest
    {
        public string Task { get; set; } = string.Empty;
        public int? StudySessionId { get; set; }
        public int Order { get; set; } = 0;
    }

    public class UpdateTodoRequest
    {
        public string? Task { get; set; }
        public bool? IsCompleted { get; set; }
        public int? Order { get; set; }
    }
}
