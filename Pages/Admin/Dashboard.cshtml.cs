using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalProducts { get; set; }

    public int PendingProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int RejectedProducts { get; set; }

    public IList<AdminAction> RecentActions { get; set; }
        = new List<AdminAction>();

    public async Task OnGetAsync()
    {
        TotalProducts = await _context.Products.CountAsync();

        PendingProducts = await _context.Products
            .CountAsync(product =>
                product.Status == ProductStatus.Pending);

        ActiveProducts = await _context.Products
            .CountAsync(product =>
                product.Status == ProductStatus.Active);

        RejectedProducts = await _context.Products
            .CountAsync(product =>
                product.Status == ProductStatus.Rejected);

        RecentActions = await _context.AdminActions
            .Include(action => action.Product)
            .Include(action => action.Admin)
            .OrderByDescending(action => action.ActionDate)
            .Take(5)
            .ToListAsync();
    }
}