using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using System.Security.Claims;

namespace Suzy.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StudySessionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StudySessionController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("GetPublicSessions")]
        public async Task<IActionResult> GetPublicSessions()
        {
            var sessions = await _context.StudySessions
                .Where(s => s.IsPublic && s.IsActive && s.CurrentParticipants < s.MaxParticipants)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.TimerType,
                    s.StudyDuration,
                    s.BreakDuration,
                    s.CurrentParticipants,
                    s.MaxParticipants,
                    s.CreatedAt,
                    s.IsPublic,
                    InviteCode = (string?)null, // Don't expose invite codes for public sessions
                    CreatorName = "Host" // You can join with AspNetUsers if you want actual names
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("GetAllAvailableSessions")]
        public async Task<IActionResult> GetAllAvailableSessions()
        {
            var sessions = await _context.StudySessions
                .Where(s => s.IsActive && s.CurrentParticipants < s.MaxParticipants)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.TimerType,
                    s.StudyDuration,
                    s.BreakDuration,
                    s.CurrentParticipants,
                    s.MaxParticipants,
                    s.CreatedAt,
                    s.IsPublic,
                    InviteCode = s.IsPublic ? (string?)null : s.InviteCode, // Only show invite code for private sessions
                    CreatorName = "Host",
                    RequiresCode = !s.IsPublic
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("GetMySessions")]
        public async Task<IActionResult> GetMySessions()
        {
            var userId = _userManager.GetUserId(User);

            var sessions = await _context.StudySessions
                .Where(s => s.CreatorUserId == userId && s.IsActive)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.TimerType,
                    s.StudyDuration,
                    s.BreakDuration,
                    s.CurrentParticipants,
                    s.MaxParticipants,
                    s.IsPublic,
                    s.InviteCode,
                    s.CreatedAt
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpPost("CreateSession")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var userId = _userManager.GetUserId(User);

            var session = new StudySession
            {
                Title = request.Title,
                Description = request.Description,
                CreatorUserId = userId!,
                IsPublic = request.IsPublic,
                MaxParticipants = request.MaxParticipants,
                TimerType = request.TimerType,
                StudyDuration = request.StudyDuration,
                BreakDuration = request.BreakDuration,
                InviteCode = !request.IsPublic ? GenerateInviteCode() : null
            };

            _context.StudySessions.Add(session);
            await _context.SaveChangesAsync();

            // Add creator as first participant
            var participant = new StudySessionParticipant
            {
                StudySessionId = session.Id,
                UserId = userId!,
                IsHost = true
            };

            _context.StudySessionParticipants.Add(participant);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                sessionId = session.Id,
                inviteCode = session.InviteCode
            });
        }

        [HttpPost("JoinSession")]
        public async Task<IActionResult> JoinSession([FromBody] JoinSessionRequest request)
        {
            var userId = _userManager.GetUserId(User);
            StudySession? session = null;

            if (request.SessionId.HasValue)
            {
                session = await _context.StudySessions
                    .FirstOrDefaultAsync(s => s.Id == request.SessionId.Value && s.IsActive);

                // If session ID is provided but it's a private session, validate the invite code
                // UNLESS the user is the creator (host) of the session
                if (session != null && !session.IsPublic && session.CreatorUserId != userId)
                {
                    if (string.IsNullOrEmpty(request.InviteCode) || session.InviteCode != request.InviteCode)
                    {
                        return Ok(new { success = false, message = "Invalid invite code for private session" });
                    }
                }
            }
            else if (!string.IsNullOrEmpty(request.InviteCode))
            {
                session = await _context.StudySessions
                    .FirstOrDefaultAsync(s => s.InviteCode == request.InviteCode && s.IsActive);
            }

            if (session == null)
            {
                return Ok(new { success = false, message = "Session not found" });
            }

            // Get current active participant count
            var activeParticipantCount = await _context.StudySessionParticipants
                .CountAsync(p => p.StudySessionId == session.Id && p.LeftAt == null);

            if (activeParticipantCount >= session.MaxParticipants)
            {
                return Ok(new { success = false, message = "Session is full" });
            }

            // Check if user is already an active participant
            var existingParticipant = await _context.StudySessionParticipants
                .FirstOrDefaultAsync(p => p.StudySessionId == session.Id && p.UserId == userId);

            if (existingParticipant != null)
            {
                if (existingParticipant.LeftAt == null)
                {
                    // User is already an active participant
                    return Ok(new { success = true, sessionId = session.Id, message = "Already in session" });
                }
                else
                {
                    // User previously left, rejoin by clearing LeftAt
                    existingParticipant.LeftAt = null;
                    existingParticipant.JoinedAt = DateTime.UtcNow; // Update join time for rejoining
                }
            }
            else
            {
                // Add new participant
                var isHost = session.CreatorUserId == userId;
                var participant = new StudySessionParticipant
                {
                    StudySessionId = session.Id,
                    UserId = userId!,
                    IsHost = isHost
                };

                _context.StudySessionParticipants.Add(participant);
            }

            // Update participant count with current active participants
            session.CurrentParticipants = await _context.StudySessionParticipants
                .CountAsync(p => p.StudySessionId == session.Id && p.LeftAt == null);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, sessionId = session.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSession(int id)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.StudySessions
                .Include(s => s.Participants)
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (session == null)
            {
                return NotFound();
            }

            // Get only active participants (where LeftAt is null)
            var activeParticipants = session.Participants.Where(p => p.LeftAt == null).ToList();

            // Check if user is an active participant
            var isParticipant = activeParticipants.Any(p => p.UserId == userId);
            if (!isParticipant && !session.IsPublic)
            {
                return Forbid();
            }

            var sessionData = new
            {
                session.Id,
                session.Title,
                session.Description,
                session.TimerType,
                session.StudyDuration,
                session.BreakDuration,
                session.CurrentParticipants,
                session.MaxParticipants,
                session.IsPublic,
                IsHost = activeParticipants.Any(p => p.UserId == userId && p.IsHost),
                IsParticipant = isParticipant,
                Participants = activeParticipants.Select(p => new
                {
                    p.UserId,
                    p.IsHost,
                    p.JoinedAt,
                    Name = p.IsHost ? "Host" : "Participant" // You can enhance this with actual names
                })
            };

            return Ok(sessionData);
        }

        [HttpPost("LeaveSession/{id}")]
        public async Task<IActionResult> LeaveSession(int id)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.StudySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (session == null)
            {
                return NotFound();
            }

            var participant = await _context.StudySessionParticipants
                .FirstOrDefaultAsync(p => p.StudySessionId == id && p.UserId == userId && p.LeftAt == null);

            if (participant == null)
            {
                return Ok(new { success = false, message = "You are not in this session" });
            }

            // Record the leave time
            participant.LeftAt = DateTime.UtcNow;

            // Check if user is the host
            if (participant.IsHost)
            {
                // If host is leaving, either transfer ownership or end session
                var otherActiveParticipants = await _context.StudySessionParticipants
                    .Where(p => p.StudySessionId == id && p.UserId != userId && p.LeftAt == null)
                    .ToListAsync();

                if (otherActiveParticipants.Any())
                {
                    // Transfer ownership to first active participant
                    var newHost = otherActiveParticipants.First();
                    newHost.IsHost = true;
                }
                else
                {
                    // No other active participants, mark session as inactive
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                }
            }

            // Update participant count (count only active participants)
            session.CurrentParticipants = await _context.StudySessionParticipants
                .CountAsync(p => p.StudySessionId == id && p.LeftAt == null);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Left session successfully" });
        }

        [HttpDelete("DeleteSession/{id}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var userId = _userManager.GetUserId(User);

            var session = await _context.StudySessions
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

            if (session == null)
            {
                return NotFound();
            }

            // Check if user is the host
            var hostParticipant = await _context.StudySessionParticipants
                .FirstOrDefaultAsync(p => p.StudySessionId == id && p.UserId == userId && p.IsHost);

            if (hostParticipant == null)
            {
                return Forbid("Only the session host can delete the session");
            }

            // Remove all participants
            var participants = await _context.StudySessionParticipants
                .Where(p => p.StudySessionId == id)
                .ToListAsync();

            _context.StudySessionParticipants.RemoveRange(participants);

            // Remove all todos associated with this session
            var todos = await _context.TodoItems
                .Where(t => t.StudySessionId == id)
                .ToListAsync();

            _context.TodoItems.RemoveRange(todos);

            // Mark session as inactive/deleted
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.CurrentParticipants = 0;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Session deleted successfully" });
        }

        [HttpPost("StartSession/{sessionId}")]
        public async Task<IActionResult> StartSession(int sessionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Unknown User";

            var session = await _context.StudySessions.FindAsync(sessionId);

            if (session == null)
                return NotFound(new { success = false, message = "Session not found" });

            // Check if user is a participant in this session
            var participant = await _context.StudySessionParticipants
                .FirstOrDefaultAsync(p => p.StudySessionId == sessionId && p.UserId == userId);

            if (participant == null)
                return Forbid("You are not a participant in this session");

            // Check if user already has an active timer session
            var activeTimerSession = await _context.StudyTimerSessions
                .FirstOrDefaultAsync(t => t.StudySessionId == sessionId &&
                                         t.UserId == userId &&
                                         t.EndTime == null);

            if (activeTimerSession != null)
                return BadRequest(new { success = false, message = "You already have an active timer session" });

            // Create new timer session
            var timerSession = new StudyTimerSession
            {
                StudySessionId = sessionId,
                UserId = userId,
                UserName = userName,
                StartTime = DateTime.UtcNow,
                SessionType = TimerSessionType.Study,
                Notes = "Timer started by user"
            };

            _context.StudyTimerSessions.Add(timerSession);

            // Update participant's last activity
            participant.LastActivityAt = DateTime.UtcNow;

            // Update session's StartedAt if this is the first timer session
            if (session.StartedAt == null)
            {
                session.StartedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Timer session started",
                timerSessionId = timerSession.Id,
                startedAt = timerSession.StartTime
            });
        }

        [HttpPost("EndSession/{sessionId}")]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var session = await _context.StudySessions.FindAsync(sessionId);

            if (session == null)
                return NotFound(new { success = false, message = "Session not found" });

            // Find the user's active timer session
            var activeTimerSession = await _context.StudyTimerSessions
                .FirstOrDefaultAsync(t => t.StudySessionId == sessionId &&
                                         t.UserId == userId &&
                                         t.EndTime == null);

            if (activeTimerSession == null)
                return BadRequest(new { success = false, message = "No active timer session found" });

            // End the timer session
            activeTimerSession.EndTime = DateTime.UtcNow;
            activeTimerSession.DurationMinutes = (int)(activeTimerSession.EndTime.Value - activeTimerSession.StartTime).TotalMinutes;
            activeTimerSession.IsCompleted = true;
            activeTimerSession.Notes = "Timer stopped by user";

            // Update participant's total study time and last activity
            var participant = await _context.StudySessionParticipants
                .FirstOrDefaultAsync(p => p.StudySessionId == sessionId && p.UserId == userId);

            if (participant != null)
            {
                // Only add to study time if this was a study session, not a break session
                if (activeTimerSession.SessionType == TimerSessionType.Study)
                {
                    participant.TotalStudyTimeMinutes += activeTimerSession.DurationMinutes;
                }
                participant.LastActivityAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Timer session ended",
                endedAt = activeTimerSession.EndTime,
                durationMinutes = activeTimerSession.DurationMinutes,
                totalStudyTimeMinutes = participant?.TotalStudyTimeMinutes ?? 0
            });
        }

        [HttpPost("StartBreak/{sessionId}")]
        public async Task<IActionResult> StartBreak(int sessionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var userName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "Unknown User";

            var session = await _context.StudySessions.FindAsync(sessionId);

            if (session == null)
                return NotFound(new { success = false, message = "Session not found" });

            // Check if user is a participant in this session
            var participant = await _context.StudySessionParticipants
                .FirstOrDefaultAsync(p => p.StudySessionId == sessionId && p.UserId == userId);

            if (participant == null)
                return Forbid("You are not a participant in this session");

            // Check if user already has an active timer session
            var activeTimerSession = await _context.StudyTimerSessions
                .FirstOrDefaultAsync(t => t.StudySessionId == sessionId &&
                                         t.UserId == userId &&
                                         t.EndTime == null);

            if (activeTimerSession != null)
                return BadRequest(new { success = false, message = "You already have an active timer session" });

            // Create new break timer session
            var timerSession = new StudyTimerSession
            {
                StudySessionId = sessionId,
                UserId = userId,
                UserName = userName,
                StartTime = DateTime.UtcNow,
                SessionType = TimerSessionType.Break,
                Notes = "Break timer started by user"
            };

            _context.StudyTimerSessions.Add(timerSession);

            // Update participant's last activity
            participant.LastActivityAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Break timer started",
                timerSessionId = timerSession.Id,
                startedAt = timerSession.StartTime
            });
        }

        [HttpGet("GetTimerStats/{sessionId}")]
        public async Task<IActionResult> GetTimerStats(int sessionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var session = await _context.StudySessions.FindAsync(sessionId);

            if (session == null)
                return NotFound(new { success = false, message = "Session not found" });

            // Get participant's timer sessions
            var timerSessions = await _context.StudyTimerSessions
                .Where(t => t.StudySessionId == sessionId && t.UserId == userId)
                .OrderBy(t => t.StartTime)
                .ToListAsync();

            // Get participant info
            var participant = await _context.StudySessionParticipants
                .FirstOrDefaultAsync(p => p.StudySessionId == sessionId && p.UserId == userId);

            // Calculate stats
            var studySessions = timerSessions.Where(t => t.SessionType == TimerSessionType.Study).ToList();
            var breakSessions = timerSessions.Where(t => t.SessionType == TimerSessionType.Break).ToList();

            var totalStudyTime = studySessions.Sum(t => t.DurationMinutes);
            var totalBreakTime = breakSessions.Sum(t => t.DurationMinutes);
            var completedStudySessions = studySessions.Count(t => t.IsCompleted);
            var completedBreakSessions = breakSessions.Count(t => t.IsCompleted);

            // Check for active session
            var activeSession = timerSessions.FirstOrDefault(t => t.EndTime == null);

            return Ok(new
            {
                success = true,
                sessionId = sessionId,
                totalStudyTimeMinutes = totalStudyTime,
                totalBreakTimeMinutes = totalBreakTime,
                completedStudySessions = completedStudySessions,
                completedBreakSessions = completedBreakSessions,
                totalSessions = timerSessions.Count,
                activeSession = activeSession != null ? new
                {
                    id = activeSession.Id,
                    sessionType = activeSession.SessionType.ToString(),
                    startTime = activeSession.StartTime,
                    elapsedMinutes = (int)(DateTime.UtcNow - activeSession.StartTime).TotalMinutes
                } : null,
                participantTotalStudyTime = participant?.TotalStudyTimeMinutes ?? 0,
                sessions = timerSessions.Select(t => new
                {
                    id = t.Id,
                    sessionType = t.SessionType.ToString(),
                    startTime = t.StartTime,
                    endTime = t.EndTime,
                    durationMinutes = t.DurationMinutes,
                    isCompleted = t.IsCompleted,
                    notes = t.Notes
                })
            });
        }

        private string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpPost("RecalculateParticipantStudyTimes")]
        public async Task<IActionResult> RecalculateParticipantStudyTimes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // Get all participants for this user
            var participants = await _context.StudySessionParticipants
                .Where(p => p.UserId == userId)
                .ToListAsync();

            int updatedCount = 0;

            foreach (var participant in participants)
            {
                // Calculate the actual study time from timer sessions
                var studyTimerSessions = await _context.StudyTimerSessions
                    .Where(t => t.StudySessionId == participant.StudySessionId &&
                               t.UserId == userId &&
                               t.SessionType == TimerSessionType.Study &&
                               t.EndTime != null)
                    .ToListAsync();

                var correctStudyTime = studyTimerSessions.Sum(t => t.DurationMinutes);

                // Update if different
                if (participant.TotalStudyTimeMinutes != correctStudyTime)
                {
                    participant.TotalStudyTimeMinutes = correctStudyTime;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = $"Recalculated study times for {updatedCount} participant records",
                updatedCount
            });
        }
    }

    public class CreateSessionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; }
        public int MaxParticipants { get; set; } = 10;
        public TimerType TimerType { get; set; }
        public int StudyDuration { get; set; } = 25;
        public int BreakDuration { get; set; } = 5;
    }

    public class JoinSessionRequest
    {
        public int? SessionId { get; set; }
        public string? InviteCode { get; set; }
    }
}
