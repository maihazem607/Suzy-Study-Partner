using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Suzy.Pages.Sessions
{
    public class StudySessionsModel : PageModel
    {
        private readonly ILogger<StudySessionsModel> _logger;

        public StudySessionsModel(ILogger<StudySessionsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Study Sessions page accessed");
        }

        public IActionResult OnPostAddTask(string taskText)
        {
            if (!string.IsNullOrWhiteSpace(taskText))
            {
                // Add logic to handle new tasks
                _logger.LogInformation("New task added: {TaskText}", taskText);
            }

            return Page();
        }

        public IActionResult OnPostToggleTask(int taskId)
        {
            // Add logic to toggle task completion
            _logger.LogInformation("Task {TaskId} toggled", taskId);

            return Page();
        }
    }
}
