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
            var utcToday = DateTime.UtcNow.Date;
            var analytics = await _context.StudyAnalytics
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date.Date == utcToday);

            // Check if there are newer timer sessions or todos
            var lastTimerSession = await _context.StudyTimerSessions
                .Where(t => t.UserId == userId && t.StartTime.Date == utcToday)
                .OrderByDescending(t => t.StartTime)
                .FirstOrDefaultAsync();

            var lastTodo = await _context.TodoItems
                .Where(t => t.UserId == userId && t.CreatedAt.Date == utcToday)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (analytics == null ||
                (lastTimerSession != null && lastTimerSession.StartTime > analytics.CreatedAt) ||
                (lastTodo != null && lastTodo.CreatedAt > analytics.CreatedAt))
            {
                if (analytics != null)
                {
                    _context.StudyAnalytics.Remove(analytics); // Remove stale analytics
                    await _context.SaveChangesAsync();
                }
                analytics = await GenerateTodayAnalyticsAsync(userId);
            }

            return analytics;
        }

        public async Task<WeeklySummary> GetWeeklySummaryAsync(string userId)
        {
            var utcToday = DateTime.UtcNow.Date;
            // Look at the past 7 days instead of calendar week
            var sevenDaysAgo = utcToday.AddDays(-6); // Today + 6 previous days = 7 days total
            var summary = await _context.WeeklySummaries
                .FirstOrDefaultAsync(w => w.UserId == userId && w.WeekStartDate.Date == sevenDaysAgo);

            // Check if there are newer timer sessions or todos since last generation
            var lastTimerSession = await _context.StudyTimerSessions
                .Where(t => t.UserId == userId && t.StartTime >= sevenDaysAgo)
                .OrderByDescending(t => t.StartTime)
                .FirstOrDefaultAsync();

            var lastTodo = await _context.TodoItems
                .Where(t => t.UserId == userId && t.CreatedAt >= sevenDaysAgo)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();

            if (summary == null ||
                (lastTimerSession != null && lastTimerSession.StartTime > summary.GeneratedAt) ||
                (lastTodo != null && lastTodo.CreatedAt > summary.GeneratedAt))
            {
                if (summary != null)
                {
                    _context.WeeklySummaries.Remove(summary); // Remove stale summary
                    await _context.SaveChangesAsync();
                }
                summary = await GenerateWeeklySummaryAsync(userId);
            }

            return summary;
        }

        public async Task UpdateDailyAnalyticsAsync(string userId)
        {
            var utcToday = DateTime.UtcNow.Date;

            // Find existing analytics for today
            var existingAnalytics = await _context.StudyAnalytics
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Date.Date == utcToday);

            if (existingAnalytics != null)
            {
                // Update existing analytics
                await UpdateExistingAnalyticsAsync(existingAnalytics, userId);
            }
            else
            {
                // Generate new analytics for today
                await GenerateTodayAnalyticsAsync(userId);
            }
        }

        private async Task UpdateExistingAnalyticsAsync(StudyAnalytics analytics, string userId)
        {
            var utcToday = DateTime.UtcNow.Date;

            // Get todos for today (UTC)
            var todayTodos = await _context.TodoItems
                .Where(t => t.UserId == userId && t.CreatedAt.Date == utcToday)
                .ToListAsync();

            // Update todo-related analytics
            analytics.CompletedTodos = todayTodos.Count(t => t.IsCompleted);
            analytics.TotalTodos = todayTodos.Count;

            // Recalculate study and break time from timer sessions
            var todayTimerSessions = await _context.StudyTimerSessions
                .Where(t => t.UserId == userId && t.StartTime.Date == utcToday)
                .ToListAsync();

            var totalStudyMinutes = 0;
            var totalBreakMinutes = 0;

            foreach (var timerSession in todayTimerSessions)
            {
                var duration = timerSession.EndTime.HasValue
                    ? timerSession.DurationMinutes // Use stored duration for completed sessions
                    : (int)(DateTime.UtcNow - timerSession.StartTime).TotalMinutes; // Calculate for ongoing

                if (timerSession.SessionType == TimerSessionType.Study)
                {
                    totalStudyMinutes += Math.Max(0, duration);
                }
                else if (timerSession.SessionType == TimerSessionType.Break)
                {
                    totalBreakMinutes += Math.Max(0, duration);
                }
            }

            analytics.TotalStudyMinutes = totalStudyMinutes;
            analytics.TotalBreakMinutes = totalBreakMinutes;

            await _context.SaveChangesAsync();
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
            try
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
            catch (Exception ex)
            {
                // Log the error (in a real app, you'd use proper logging)
                Console.WriteLine($"Error in ProcessUserMessageAsync: {ex.Message}");

                // Return a friendly error message
                return "I'm sorry, I'm having trouble processing your message right now. This might be because I'm still learning about your mock exam data. Please try again in a moment, or try asking about a different topic! 😊";
            }
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
            var utcToday = DateTime.UtcNow.Date;

            // Get timer sessions for today (UTC)
            var todayTimerSessions = await _context.StudyTimerSessions
                .Where(t => t.UserId == userId && t.StartTime.Date == utcToday)
                .ToListAsync();

            // Get todos for today (UTC)
            var todayTodos = await _context.TodoItems
                .Where(t => t.UserId == userId && t.CreatedAt.Date == utcToday)
                .ToListAsync();

            // Calculate study and break time from timer sessions
            var totalStudyMinutes = 0;
            var totalBreakMinutes = 0;

            foreach (var timerSession in todayTimerSessions)
            {
                var duration = timerSession.EndTime.HasValue
                    ? timerSession.DurationMinutes // Use stored duration for completed sessions
                    : (int)(DateTime.UtcNow - timerSession.StartTime).TotalMinutes; // Calculate for ongoing

                if (timerSession.SessionType == TimerSessionType.Study)
                {
                    totalStudyMinutes += Math.Max(0, duration);
                }
                else if (timerSession.SessionType == TimerSessionType.Break)
                {
                    totalBreakMinutes += Math.Max(0, duration);
                }
            }

            var analytics = new StudyAnalytics
            {
                UserId = userId,
                Date = utcToday,
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
            var utcToday = DateTime.UtcNow.Date;
            // Look at the past 7 days instead of calendar week
            var sevenDaysAgo = utcToday.AddDays(-6); // Today + 6 previous days = 7 days total
            var endDate = utcToday.AddDays(1); // Include today

            // Try to get existing analytics for the past 7 days
            var weeklyAnalytics = await _context.StudyAnalytics
                .Where(a => a.UserId == userId && a.Date >= sevenDaysAgo && a.Date < endDate)
                .ToListAsync();

            // If no analytics exist, calculate directly from timer sessions
            var totalStudyMinutes = 0;
            var totalBreakMinutes = 0;
            var daysWithStudyData = new HashSet<DateTime>();

            if (!weeklyAnalytics.Any())
            {
                var weekTimerSessions = await _context.StudyTimerSessions
                    .Where(t => t.UserId == userId && t.StartTime >= sevenDaysAgo && t.StartTime < endDate)
                    .ToListAsync();

                foreach (var timerSession in weekTimerSessions)
                {
                    var duration = timerSession.EndTime.HasValue
                        ? timerSession.DurationMinutes // Use stored duration for completed sessions
                        : (int)(DateTime.UtcNow - timerSession.StartTime).TotalMinutes; // Calculate for ongoing

                    if (timerSession.SessionType == TimerSessionType.Study)
                    {
                        totalStudyMinutes += Math.Max(0, duration);
                        daysWithStudyData.Add(timerSession.StartTime.Date);
                    }
                    else if (timerSession.SessionType == TimerSessionType.Break)
                    {
                        totalBreakMinutes += Math.Max(0, duration);
                    }
                }
            }
            else
            {
                totalStudyMinutes = weeklyAnalytics.Sum(a => a.TotalStudyMinutes);
                totalBreakMinutes = weeklyAnalytics.Sum(a => a.TotalBreakMinutes);
                // Count days that have study data from analytics
                daysWithStudyData = weeklyAnalytics
                    .Where(a => a.TotalStudyMinutes > 0)
                    .Select(a => a.Date.Date)
                    .ToHashSet();
            }

            var weeklyTodos = await _context.TodoItems
                .Where(t => t.UserId == userId && t.CreatedAt >= sevenDaysAgo && t.CreatedAt < endDate)
                .ToListAsync();

            // Calculate average based on days with actual study data, or use 7 days if no study data
            var averageStudyTimePerDay = daysWithStudyData.Count > 0
                ? totalStudyMinutes / (double)daysWithStudyData.Count
                : totalStudyMinutes / 7.0;

            var summary = new WeeklySummary
            {
                UserId = userId,
                WeekStartDate = sevenDaysAgo, // Changed to represent start of 7-day period
                TotalStudyMinutes = totalStudyMinutes,
                TotalBreakMinutes = totalBreakMinutes,
                AverageStudyTimePerDay = averageStudyTimePerDay,
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
            var utcToday = DateTime.UtcNow.Date;
            var weekStart = utcToday.AddDays(-(int)utcToday.DayOfWeek);

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
                        .Where(t => t.UserId == userId && t.CreatedAt.Date == utcToday)
                        .ToListAsync(),
                    WeeklyTodos = await _context.TodoItems
                        .Where(t => t.UserId == userId && t.CreatedAt >= weekStart)
                        .ToListAsync()
                },
                ChatPathType.WeeklySummary => await GetWeeklySummaryAsync(userId),
                ChatPathType.MockExamReview => new
                {
                    RecentMockTests = await GetRecentMockTestsAsync(userId),
                    OverallStats = await GetMockTestOverallStatsAsync(userId)
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
- Keep responses detailed and structured with multiple paragraphs
- Use a friendly, encouraging tone
- Include relevant emojis (✅ 📊 🕐 🔁 ❗ 📈 📉 💪 🎯)
- Provide comprehensive analysis with specific data points
- End with actionable suggestions when appropriate
- For mock exam analysis, be thorough and analytical

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
                1 => @"Analyze their mock exam performance comprehensively. Focus on:
                     - Overall performance trends and scores
                     - Subject-wise strengths and weaknesses  
                     - Comparison with their historical performance
                     - Identification of consistent problem areas
                     - Time management and accuracy patterns
                     Provide specific insights with percentages and concrete examples from their recent tests.",

                2 => @"Based on their mock exam analysis, provide targeted improvement strategies:
                     - Specific study recommendations for weak subject areas
                     - Techniques to improve accuracy in problem areas
                     - Study schedule suggestions focusing on identified gaps
                     - Practice strategies for consistent weak question types
                     - Goal setting for next mock exams",

                3 => @"Create a comprehensive study plan and offer to generate targeted practice:
                     - Weekly study schedule addressing weak areas
                     - Specific topics to focus on based on their mistakes
                     - Offer to create custom quiz questions for their problem areas
                     - Set measurable goals for improvement
                     - Suggest timeline for next mock exam",

                _ => "Provide encouragement, summarize key insights, and motivate them for continued improvement."
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

        private async Task<object> GetRecentMockTestsAsync(string userId)
        {
            try
            {
                var recentTests = await _context.MockTestResults
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(5)
                    .ToListAsync();

                var result = new List<object>();

                foreach (var test in recentTests)
                {
                    var questions = await _context.MockTestQuestions
                        .Where(q => q.MockTestResultId == test.Id)
                        .ToListAsync();

                    result.Add(new
                    {
                        test.Id,
                        test.Subject,
                        test.Timestamp,
                        test.Score,
                        test.TotalQuestions,
                        ScorePercentage = test.TotalQuestions > 0 ? (double)test.Score / test.TotalQuestions * 100 : 0,
                        CorrectAnswers = questions.Count(q => q.IsCorrect),
                        IncorrectAnswers = questions.Count(q => !q.IsCorrect),
                        WeakAreas = questions
                            .Where(q => !q.IsCorrect)
                            .Select(q => q.QuestionText.Length > 100 ? q.QuestionText.Substring(0, 100) + "..." : q.QuestionText)
                            .Take(3)
                            .ToList(),
                        StrongAreas = questions
                            .Where(q => q.IsCorrect)
                            .Select(q => q.QuestionText.Length > 100 ? q.QuestionText.Substring(0, 100) + "..." : q.QuestionText)
                            .Take(3)
                            .ToList()
                    });
                }

                return result;
            }
            catch (Exception)
            {
                return new List<object>(); // Return empty list if there's an error
            }
        }

        private async Task<object> GetMockTestOverallStatsAsync(string userId)
        {
            try
            {
                var allTests = await _context.MockTestResults
                    .Where(m => m.UserId == userId)
                    .ToListAsync();

                if (!allTests.Any())
                {
                    return new
                    {
                        TotalTestsTaken = 0,
                        AverageScore = 0.0,
                        BestScore = 0.0,
                        HasData = false,
                        Message = "No mock test data available yet. Take your first mock exam to get started!"
                    };
                }

                var testsWithQuestions = allTests.Where(t => t.TotalQuestions > 0).ToList();
                var scores = testsWithQuestions.Select(t => (double)t.Score / t.TotalQuestions * 100).ToList();

                return new
                {
                    TotalTestsTaken = allTests.Count,
                    AverageScore = scores.Any() ? scores.Average() : 0.0,
                    BestScore = scores.Any() ? scores.Max() : 0.0,
                    HasData = allTests.Any(),
                    SubjectBreakdown = allTests
                        .GroupBy(t => t.Subject ?? "Unknown")
                        .Select(g => new
                        {
                            Subject = g.Key,
                            TestCount = g.Count(),
                            AverageScore = g.Where(t => t.TotalQuestions > 0)
                                           .Select(t => (double)t.Score / t.TotalQuestions * 100)
                                           .DefaultIfEmpty(0)
                                           .Average(),
                            LastTestDate = g.Max(t => t.Timestamp)
                        })
                        .ToList()
                };
            }
            catch (Exception)
            {
                return new
                {
                    TotalTestsTaken = 0,
                    AverageScore = 0.0,
                    BestScore = 0.0,
                    HasData = false,
                    Message = "Unable to retrieve mock test statistics at the moment."
                };
            }
        }
    }
}
