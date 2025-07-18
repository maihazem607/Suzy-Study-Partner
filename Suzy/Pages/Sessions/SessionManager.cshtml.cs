using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Suzy.Pages.Sessions
{
    [Authorize]
    public class SessionManagerModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
