using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Suzy.Services;
using System.Security.Claims;

namespace Suzy.Pages.ChatWithSuzy
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ChatAnalyticsService _chatAnalyticsService;

        public IndexModel(ChatAnalyticsService chatAnalyticsService)
        {
            _chatAnalyticsService = chatAnalyticsService;
        }

        public void OnGet()
        {
        }
    }
}
