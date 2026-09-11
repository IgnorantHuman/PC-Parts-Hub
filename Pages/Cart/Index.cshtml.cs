using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Cart;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<CartItem> CartItems { get; set; } = new();

    // Use CurrentPrice so an active promotion is included.
    public decimal Subtotal =>
        CartItems.Sum(item =>
            (item.Product?.CurrentPrice ?? 0) *
            item.Quantity);

    public async Task OnGetAsync()
    {
        string buyerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!;

        CartItems = await _context.CartItems
            .Include(item => item.Product)
            .Where(item => item.BuyerId == buyerId)
            .ToListAsync();
    }

    public async Task<IActionResult>
        OnPostUpdateQuantityAsync(
            int cartItemId,
            int quantity)
    {
        string buyerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!;

        CartItem? item = await _context.CartItems
            .Include(item => item.Product)
            .FirstOrDefaultAsync(item =>
                item.Id == cartItemId &&
                item.BuyerId == buyerId);

        if (item == null || item.Product == null)
        {
            if (IsAjaxRequest())
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Cart item was not found."
                })
                {
                    StatusCode = 404
                };
            }

            return RedirectToPage();
        }

        if (quantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else
        {
            item.Quantity =
                Math.Min(
                    quantity,
                    item.Product.Stock);
        }

        await _context.SaveChangesAsync();

        if (IsAjaxRequest())
        {
            return await CartJsonAsync(buyerId);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult>
        OnPostRemoveAsync(int cartItemId)
    {
        string buyerId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)!;

        CartItem? item = await _context.CartItems
            .FirstOrDefaultAsync(item =>
                item.Id == cartItemId &&
                item.BuyerId == buyerId);

        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        if (IsAjaxRequest())
        {
            return await CartJsonAsync(buyerId);
        }

        return RedirectToPage();
    }

    private async Task<JsonResult>
        CartJsonAsync(string buyerId)
    {
        List<CartItem> items =
            await _context.CartItems
                .Include(item => item.Product)
                .Where(item =>
                    item.BuyerId == buyerId)
                .ToListAsync();

        return new JsonResult(new
        {
            success = true,

            cartCount = items.Sum(item =>
                item.Quantity),

            // Use promotional price for Ajax total.
            total = items.Sum(item =>
                (item.Product?.CurrentPrice ?? 0) *
                item.Quantity),

            items = items.Select(item => new
            {
                id = item.Id,

                quantity = item.Quantity,

                // Use promotional price for each subtotal.
                subtotal =
                    (item.Product?.CurrentPrice ?? 0) *
                    item.Quantity
            })
        });
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"]
            == "XMLHttpRequest";
    }
}