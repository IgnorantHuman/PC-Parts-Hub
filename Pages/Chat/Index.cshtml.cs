using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PCShop.Pages.Chat;

[Authorize]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}