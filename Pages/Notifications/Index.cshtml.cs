using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Notifications;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Notification> Notifications { get; set; } = new();

    public async Task OnGetAsync()
    {
        string userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        Notifications = await _context.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        string userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        Notification? notification =
            await _context.Notifications
                .FirstOrDefaultAsync(notification =>
                    notification.Id == id &&
                    notification.UserId == userId);

        if (notification == null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(notification.Link) &&
            Url.IsLocalUrl(notification.Link))
        {
            return LocalRedirect(notification.Link);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        string userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        List<Notification> unreadNotifications =
            await _context.Notifications
                .Where(notification =>
                    notification.UserId == userId &&
                    !notification.IsRead)
                .ToListAsync();

        foreach (Notification notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "All notifications were marked as read.";

        return RedirectToPage();
    }
}