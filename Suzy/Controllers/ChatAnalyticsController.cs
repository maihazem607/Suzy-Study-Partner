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
    [ApiController]
    [Route("api/[controller]")]
    public class ChatAnalyticsController : ControllerBase
    {
        private readonly ChatAnalyticsService _chatAnalyticsService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ChatAnalyticsController(ChatAnalyticsService chatAnalyticsService, UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _chatAnalyticsService = chatAnalyticsService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet("analytics/today")]
        public async Task<IActionResult> GetTodayAnalytics()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var analytics = await _chatAnalyticsService.GetTodayAnalyticsAsync(userId);
            return Ok(analytics);
        }

        [HttpGet("analytics/weekly")]
        public async Task<IActionResult> GetWeeklySummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var summary = await _chatAnalyticsService.GetWeeklySummaryAsync(userId);
            return Ok(summary);
        }

        [HttpGet("paths")]
        public IActionResult GetAvailableChatPaths()
        {
            var paths = _chatAnalyticsService.GetAvailableChatPaths();
            return Ok(paths);
        }

        [HttpPost("conversation/start")]
        public async Task<IActionResult> StartConversation([FromBody] ChatPathRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var conversation = await _chatAnalyticsService.StartConversationAsync(userId, request.PathType);
            return Ok(new { id = conversation.Id });
        }

        [HttpPost("conversation/{conversationId}/message")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] ChatMessageRequest request)
        {
            try
            {
                var response = await _chatAnalyticsService.ProcessUserMessageAsync(conversationId, request.Message);
                return Ok(new { response });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("debug/user-data")]
        public async Task<IActionResult> GetUserDebugData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var today = DateTime.Today;
            var utcToday = DateTime.UtcNow.Date;

            var studySessions = await _context.StudySessions
                .Where(s => s.CreatorUserId == userId)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.StartedAt,
                    s.EndedAt,
                    s.BreakDuration,
                    s.CreatorUserId,
                    StartedAtDate = s.StartedAt.HasValue ? s.StartedAt.Value.Date : (DateTime?)null,
                    DurationMinutes = s.StartedAt.HasValue && s.EndedAt.HasValue ?
                        (s.EndedAt.Value - s.StartedAt.Value).TotalMinutes :
                        (s.StartedAt.HasValue ? (DateTime.UtcNow - s.StartedAt.Value).TotalMinutes : 0)
                })
                .ToListAsync();

            var todoItems = await _context.TodoItems
                .Where(t => t.UserId == userId)
                .Select(t => new
                {
                    t.Id,
                    t.Task,
                    t.IsCompleted,
                    t.CreatedAt,
                    t.UserId,
                    CreatedAtDate = t.CreatedAt.Date
                })
                .ToListAsync();

            var existingAnalytics = await _context.StudyAnalytics
                .Where(a => a.UserId == userId)
                .ToListAsync();

            return Ok(new
            {
                userId,
                today,
                utcToday,
                studySessions,
                todoItems,
                existingAnalytics,
                totalSessionsToday = studySessions.Count(s => s.StartedAtDate == today || s.StartedAtDate == utcToday),
                totalStudyMinutesToday = studySessions
                    .Where(s => s.StartedAtDate == today || s.StartedAtDate == utcToday)
                    .Sum(s => s.DurationMinutes)
            });
        }

        [HttpPost("debug/force-regenerate")]
        public async Task<IActionResult> ForceRegenerateAnalytics()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // Delete existing analytics for today to force regeneration
            var today = DateTime.Today;
            var existingAnalytics = await _context.StudyAnalytics
                .Where(a => a.UserId == userId && a.Date.Date == today)
                .ToListAsync();

            if (existingAnalytics.Any())
            {
                _context.StudyAnalytics.RemoveRange(existingAnalytics);
                await _context.SaveChangesAsync();
            }

            // Force regenerate analytics
            var newAnalytics = await _chatAnalyticsService.GetTodayAnalyticsAsync(userId);

            return Ok(new
            {
                message = "Analytics regenerated",
                analytics = newAnalytics
            });
        }
    }

    public class ChatPathRequest
    {
        public ChatPathType PathType { get; set; }
    }

    public class ChatMessageRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
