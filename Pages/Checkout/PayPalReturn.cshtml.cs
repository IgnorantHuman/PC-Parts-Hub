using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;
using PCShop.Services;

namespace PCShop.Pages.Checkout;

[Authorize]
public class PayPalReturnModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly PayPalService _payPalService;

    public PayPalReturnModel(ApplicationDbContext context, PayPalService payPalService)
    {
        _context = context;
        _payPalService = payPalService;
    }

    public async Task<IActionResult> OnGetAsync(int orderId, string token)
    {
        string buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Order? order = await _context.Orders.Include(o => o.OrderItems)
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == buyerId);
        if (order == null || order.PayPalOrderId != token) return NotFound();
        if (order.PaymentStatus == "Completed")
            return RedirectToPage("/Checkout/Success", new { orderId });

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            foreach (OrderItem item in order.OrderItems)
            {
                if (item.Product == null || item.Product.Stock < item.Quantity)
                {
                    TempData["ErrorMessage"] = $"{item.Product?.Name ?? "A product"} no longer has enough stock.";
                    return RedirectToPage("/Cart/Index");
                }
            }

            string captureId = await _payPalService.CaptureOrderAsync(token);
            foreach (OrderItem item in order.OrderItems) item.Product!.Stock -= item.Quantity;
            List<int> productIds = order.OrderItems.Select(item => item.ProductId).ToList();
            List<CartItem> cartItems = await _context.CartItems
                .Where(item => item.BuyerId == buyerId && productIds.Contains(item.ProductId)).ToListAsync();
            _context.CartItems.RemoveRange(cartItems);
            order.PayPalCaptureId = captureId;
            order.PaymentStatus = "Completed";
            order.Status = OrderStatus.Processing;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return RedirectToPage("/Checkout/Success", new { orderId });
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "PayPal payment could not be confirmed. Please try again.";
            return RedirectToPage("/Buyer/Orders/Index");
        }
    }
}
