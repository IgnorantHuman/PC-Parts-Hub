using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;

namespace PCShop.Pages.Checkout;

[Authorize]
public class PayPalCancelModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public PayPalCancelModel(ApplicationDbContext context) => _context = context;

    public async Task OnGetAsync(int orderId)
    {
        string buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var order = await _context.Orders.FirstOrDefaultAsync(o =>
            o.Id == orderId && o.BuyerId == buyerId && o.PaymentStatus == "Pending");
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
        }
    }
}
