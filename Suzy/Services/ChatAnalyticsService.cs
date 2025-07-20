using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Models;
using System.Text.Json;

namespace Suzy.Services
{
    public class ChatAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly GeminiService _geminiService;

        public ChatAnalyticsService(ApplicationDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        public async Task<StudyAnalytics> GetTodayAnalyticsAsync(string userId)
        {
            var today = DateTime.Today;
            var analytics = await _context.StudyAnalytics
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date.Date == today);

            if (analytics == null)
            {
                analytics = await GenerateTodayAnalyticsAsync(userId);
            }

            return analytics;
        }

        public async Task<WeeklySummary> GetWeeklySummaryAsync(string userId)
        {
            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var summary = await _context.WeeklySummaries
                .FirstOrDefaultAsync(w => w.UserId == userId && w.WeekStartDate.Date == weekStart);

            if (summary == null)
            {
                summary = await GenerateWeeklySummaryAsync(userId);
            }

            return summary;
        }

        public async Task<ChatConversation> StartConversationAsync(string userId, ChatPathType pathType)
        {
            var conversation = new ChatConversation
            {
                UserId = userId,
                PathType = pathType,
                CurrentStep = 1
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            return conversation;
        }

        public async Task<string> ProcessUserMessageAsync(int conversationId, string userMessage)
        {
            var conversation = await _context.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
                throw new ArgumentException("Conversation not found");

            // Add user message
            var userChatMessage = new ChatMessage
            {
                ConversationId = conversationId,
                Content = userMessage,
                IsFromUser = true
            };

            _context.ChatMessages.Add(userChatMessage);

            // Get user data context
            var dataContext = await GetUserDataContextAsync(conversation.UserId, conversation.PathType);

            // Generate Suzy's response
            var prompt = BuildPromptForPath(conversation.PathType, conversation.CurrentStep, userMessage, dataContext);
            var geminiResponse = await _geminiService.GenerateContentWithRetryAsync(prompt);

            // Parse and clean the response
            var suzyResponse = ExtractTextFromGeminiResponse(geminiResponse);

            // Add Suzy's message
            var suzyMessage = new ChatMessage
            {
                ConversationId = conversationId,
                Content = suzyResponse,
                IsFromUser = false,
                DataContext = JsonSerializer.Serialize(dataContext)
            };

            _context.ChatMessages.Add(suzyMessage);

            // Update conversation step
            conversation.CurrentStep++;

            // Check if conversation is completed (based on path length)
            var chatPath = GetChatPath(conversation.PathType);
            if (conversation.CurrentStep > chatPath.Questions.Count)
            {
                conversation.IsCompleted = true;
                conversation.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return suzyResponse;
        }

        public List<ChatPath> GetAvailableChatPaths()
        {
            return new List<ChatPath>
            {
                new ChatPath
                {
                    Type = ChatPathType.StudyTimeAnalysis,
                    Title = "Study Time Analysis",
                    Icon = "📊",
                    Description = "Get insights about your study patterns and time management",
                    Questions = new List<string>
                    {
                        "Analyze my study time.",
                        "What should I do next?",
                        "Give me a plan."
                    }
                },
                new ChatPath
                {
                    Type = ChatPathType.FocusAndPauses,
                    Title = "Focus & Pauses",
                    Icon = "🎯",
                    Description = "Learn about your focus patterns and how to improve concentration",
                    Questions = new List<string>
                    {
                        "Was I focused this week?",
                        "How do I focus better?",
                        "Okay, give me a focus checklist."
                    }
                },
                new ChatPath
                {
                    Type = ChatPathType.FlashcardProgress,
                    Title = "Flashcard Progress",
                    Icon = "🃏",
                    Description = "Review your flashcard learning progress and retention",
                    Questions = new List<string>
                    {
                        "How's my flashcard progress?",
                        "What should I do?"
                    }
                },
                new ChatPath
                {
                    Type = ChatPathType.TodoProductivity,
                    Title = "To-Do Productivity",
                    Icon = "✅",
                    Description = "Analyze your task completion and productivity habits",
                    Questions = new List<string>
                    {
                        "Did I complete my tasks?",
                        "How can I be more productive?"
                    }
                },
                new ChatPath
                {
                    Type = ChatPathType.WeeklySummary,
                    Title = "Weekly Summary",
                    Icon = "📈",
                    Description = "Get a comprehensive overview of your week's progress",
                    Questions = new List<string>
                    {
                        "Give me a weekly summary.",
                        "Okay, what now?",
                        "Yes, generate a plan."
                    }
                },
                new ChatPath
                {
                    Type = ChatPathType.MockExamReview,
                    Title = "Mock Exam Review",
                    Icon = "📝",
                    Description = "Review your mock exam performance and get targeted advice",
                    Questions = new List<string>
                    {
                        "How did I do in my last mock exam?",
                        "What should I focus on?",
                        "Yes, create quiz."
                    }
                }
            };
        }

        public ChatPath GetChatPath(ChatPathType pathType)
        {
            return GetAvailableChatPaths().First(p => p.Type == pathType);
        }

        private async Task<StudyAnalytics> GenerateTodayAnalyticsAsync(string userId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var utcToday = DateTime.UtcNow.Date;
            var utcTomorrow = utcToday.AddDays(1);

            // Get ALL timer sessions for the user (not just today) to show total study time
            var allTimerSessions = await _context.StudyTimerSessions
                .Where(t => t.UserId == userId)
                .ToListAsync();

            // Get timer sessions for today only for today's specific metrics
            var todayTimerSessions = allTimerSessions
                .Where(t => t.StartTime.Date == today || t.StartTime.Date == utcToday)
                .ToList();

            // Get todos for today (check both local and UTC dates)
            var todayTodos = await _context.TodoItems
                .Where(t => t.UserId == userId &&
                           (t.CreatedAt.Date == today || t.CreatedAt.Date == utcToday))
                .ToListAsync();

            // Calculate study and break time from timer sessions
            var totalStudyMinutes = 0;
            var totalBreakMinutes = 0;

            foreach (var timerSession in todayTimerSessions)
            {
                if (timerSession.EndTime.HasValue)
                {
                    // Completed timer session - use recorded duration
                    if (timerSession.SessionType == TimerSessionType.Study)
                    {
                        totalStudyMinutes += timerSession.DurationMinutes;
                    }
                    else if (timerSession.SessionType == TimerSessionType.Break)
                    {
                        totalBreakMinutes += timerSession.DurationMinutes;
                    }
                }
                else
                {
                    // Ongoing timer session - calculate current duration
                    var currentDuration = (int)(DateTime.UtcNow - timerSession.StartTime).TotalMinutes;
                    if (timerSession.SessionType == TimerSessionType.Study)
                    {
                        totalStudyMinutes += Math.Max(0, currentDuration);
                    }
                    else if (timerSession.SessionType == TimerSessionType.Break)
                    {
                        totalBreakMinutes += Math.Max(0, currentDuration);
                    }
                }
            }

            var analytics = new StudyAnalytics
            {
                UserId = userId,
                Date = today,
                TotalStudyMinutes = totalStudyMinutes,
                TotalBreakMinutes = totalBreakMinutes,
                CompletedTodos = todayTodos.Count(t => t.IsCompleted),
                TotalTodos = todayTodos.Count,
                FlashcardsReviewed = 0 // This would need to be tracked in flashcard system
            };

            _context.StudyAnalytics.Add(analytics);
            await _context.SaveChangesAsync();

            return analytics;
        }

        private async Task<WeeklySummary> GenerateWeeklySummaryAsync(string userId)
        {
            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            var weekEnd = weekStart.AddDays(7);

            // Try to get existing analytics for the week
            var weeklyAnalytics = await _context.StudyAnalytics
                .Where(a => a.UserId == userId && a.Date >= weekStart && a.Date < weekEnd)
                .ToListAsync();

            // If no analytics exist, calculate directly from timer sessions
            var totalStudyMinutes = 0;
            var totalBreakMinutes = 0;

            if (!weeklyAnalytics.Any())
            {
                var weekTimerSessions = await _context.StudyTimerSessions
                    .Where(t => t.UserId == userId &&
                               ((t.StartTime >= weekStart && t.StartTime < weekEnd) ||
                                (t.StartTime.Date >= weekStart && t.StartTime.Date < weekEnd)))
                    .ToListAsync();

                foreach (var timerSession in weekTimerSessions)
                {
                    if (timerSession.EndTime.HasValue)
                    {
                        // Completed timer session
                        if (timerSession.SessionType == TimerSessionType.Study)
                        {
                            totalStudyMinutes += timerSession.DurationMinutes;
                        }
                        else if (timerSession.SessionType == TimerSessionType.Break)
                        {
                            totalBreakMinutes += timerSession.DurationMinutes;
                        }
                    }
                    else
                    {
                        // Ongoing timer session - calculate current duration
                        var currentDuration = (int)(DateTime.UtcNow - timerSession.StartTime).TotalMinutes;
                        if (timerSession.SessionType == TimerSessionType.Study)
                        {
                            totalStudyMinutes += Math.Max(0, currentDuration);
                        }
                        else if (timerSession.SessionType == TimerSessionType.Break)
                        {
                            totalBreakMinutes += Math.Max(0, currentDuration);
                        }
                    }
                }
            }
            else
            {
                totalStudyMinutes = weeklyAnalytics.Sum(a => a.TotalStudyMinutes);
                totalBreakMinutes = weeklyAnalytics.Sum(a => a.TotalBreakMinutes);
            }

            var weeklyTodos = await _context.TodoItems
                .Where(t => t.UserId == userId &&
                           ((t.CreatedAt >= weekStart && t.CreatedAt < weekEnd) ||
                            (t.CreatedAt.Date >= weekStart && t.CreatedAt.Date < weekEnd)))
                .ToListAsync();

            var summary = new WeeklySummary
            {
                UserId = userId,
                WeekStartDate = weekStart,
                TotalStudyMinutes = totalStudyMinutes,
                TotalBreakMinutes = totalBreakMinutes,
                AverageStudyTimePerDay = totalStudyMinutes / 7.0, // Average over 7 days
                CompletedTodos = weeklyTodos.Count(t => t.IsCompleted),
                TotalTodos = weeklyTodos.Count,
                FlashcardsReviewed = weeklyAnalytics.Sum(a => a.FlashcardsReviewed),
                ProductivityScore = weeklyTodos.Count > 0 ?
                    (double)weeklyTodos.Count(t => t.IsCompleted) / weeklyTodos.Count * 100 : 0
            };

            _context.WeeklySummaries.Add(summary);
            await _context.SaveChangesAsync();

            return summary;
        }

        private async Task<object> GetUserDataContextAsync(string userId, ChatPathType pathType)
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);

            return pathType switch
            {
                ChatPathType.StudyTimeAnalysis => new
                {
                    TodayAnalytics = await GetTodayAnalyticsAsync(userId),
                    WeeklyAnalytics = await _context.StudyAnalytics
                        .Where(a => a.UserId == userId && a.Date >= weekStart)
                        .ToListAsync()
                },
                ChatPathType.FocusAndPauses => new
                {
                    WeeklyAnalytics = await _context.StudyAnalytics
                        .Where(a => a.UserId == userId && a.Date >= weekStart)
                        .ToListAsync(),
                    RecentSessions = await _context.StudySessions
                        .Where(s => s.CreatorUserId == userId && s.StartedAt >= weekStart)
                        .OrderByDescending(s => s.StartedAt)
                        .Take(10)
                        .ToListAsync()
                },
                ChatPathType.FlashcardProgress => new
                {
                    FlashcardsThisWeek = await _context.StudyAnalytics
                        .Where(a => a.UserId == userId && a.Date >= weekStart)
                        .SumAsync(a => a.FlashcardsReviewed)
                },
                ChatPathType.TodoProductivity => new
                {
                    TodayTodos = await _context.TodoItems
                        .Where(t => t.UserId == userId && t.CreatedAt.Date == today)
                        .ToListAsync(),
                    WeeklyTodos = await _context.TodoItems
                        .Where(t => t.UserId == userId && t.CreatedAt >= weekStart)
                        .ToListAsync()
                },
                ChatPathType.WeeklySummary => await GetWeeklySummaryAsync(userId),
                ChatPathType.MockExamReview => new
                {
                    // This would be implemented when mock exam system is added
                    Message = "Mock exam data not yet implemented"
                },
                _ => new { Message = "No data available" }
            };
        }

        private string BuildPromptForPath(ChatPathType pathType, int step, string userMessage, object dataContext)
        {
            var dataJson = JsonSerializer.Serialize(dataContext, new JsonSerializerOptions { WriteIndented = true });
            var chatPath = GetChatPath(pathType);

            var basePrompt = $@"
You are Suzy, a friendly and motivating AI study assistant. 

RESPONSE RULES:
- Keep responses under 100 words
- Use a friendly, encouraging tone
- Include relevant emojis (✅ 📊 🕐 🔁 ❗)
- Provide 1 core insight per response
- End with an actionable suggestion when appropriate

CONVERSATION CONTEXT:
- Chat Path: {chatPath.Title}
- Current Step: {step} of {chatPath.Questions.Count}
- User Message: ""{userMessage}""

USER DATA:
{dataJson}

Based on the user's data and message, provide a helpful response that follows the conversation path for {chatPath.Title}.
";

            return pathType switch
            {
                ChatPathType.StudyTimeAnalysis => basePrompt + GetStudyTimeAnalysisPrompt(step),
                ChatPathType.FocusAndPauses => basePrompt + GetFocusAndPausesPrompt(step),
                ChatPathType.FlashcardProgress => basePrompt + GetFlashcardProgressPrompt(step),
                ChatPathType.TodoProductivity => basePrompt + GetTodoProductivityPrompt(step),
                ChatPathType.WeeklySummary => basePrompt + GetWeeklySummaryPrompt(step),
                ChatPathType.MockExamReview => basePrompt + GetMockExamReviewPrompt(step),
                _ => basePrompt
            };
        }

        private string GetStudyTimeAnalysisPrompt(int step)
        {
            return step switch
            {
                1 => "Analyze their study time patterns. Focus on total time, consistency, and any notable trends.",
                2 => "Suggest specific improvements based on their study patterns.",
                3 => "Provide a concrete, actionable study plan for tomorrow or this week.",
                _ => "Provide encouragement and wrap up the conversation."
            };
        }

        private string GetFocusAndPausesPrompt(int step)
        {
            return step switch
            {
                1 => "Evaluate their focus quality this week based on study sessions and break patterns.",
                2 => "Give specific tips for improving focus and managing distractions.",
                3 => "Create a practical focus checklist they can use during study sessions.",
                _ => "Provide encouragement and wrap up the conversation."
            };
        }

        private string GetFlashcardProgressPrompt(int step)
        {
            return step switch
            {
                1 => "Review their flashcard usage and learning progress.",
                2 => "Suggest how to optimize their flashcard study routine.",
                _ => "Provide encouragement and wrap up the conversation."
            };
        }

        private string GetTodoProductivityPrompt(int step)
        {
            return step switch
            {
                1 => "Analyze their task completion rate and productivity patterns.",
                2 => "Provide specific strategies to improve productivity and task management.",
                _ => "Provide encouragement and wrap up the conversation."
            };
        }

        private string GetWeeklySummaryPrompt(int step)
        {
            return step switch
            {
                1 => "Provide a comprehensive overview of their week's study progress.",
                2 => "Suggest next steps based on their weekly performance.",
                3 => "Generate a specific plan for the upcoming week.",
                _ => "Provide encouragement and wrap up the conversation."
            };
        }

        private string GetMockExamReviewPrompt(int step)
        {
            return step switch
            {
                1 => "Review their mock exam performance (note: mock exam data not yet implemented).",
                2 => "Identify areas for improvement based on exam results.",
                3 => "Offer to create a targeted quiz for weak areas.",
                _ => "Provide encouragement and wrap up the conversation."
            };
        }

        private string ExtractTextFromGeminiResponse(string geminiResponse)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(geminiResponse);
                var candidates = jsonDoc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var content = candidates[0].GetProperty("content");
                    var parts = content.GetProperty("parts");
                    if (parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();

                        // Truncate if too long (safety measure)
                        if (text != null && text.Length > 300)
                        {
                            text = text.Substring(0, 297) + "...";
                        }

                        return text ?? "Sorry, I couldn't generate a response.";
                    }
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return a fallback response
            }

            return "Sorry, I couldn't generate a response right now. Please try again! 😊";
        }
    }
}
