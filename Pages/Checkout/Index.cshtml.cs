using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Checkout;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public IndexModel(ApplicationDbContext context) => _context = context;

    [BindProperty] public Order Order { get; set; } = new();
    [BindProperty, Required(ErrorMessage = "Please select a demo payment method.")]
    public string PaymentMethod { get; set; } = "Card";
    [BindProperty] public string? CardNumber { get; set; }
    [BindProperty] public string? CardHolder { get; set; }
    [BindProperty] public string? CardExpiry { get; set; }
    [BindProperty] public string? CardCvv { get; set; }
    [BindProperty] public string? BankName { get; set; }
    [BindProperty] public string? EWalletName { get; set; }

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalAmount => CartItems.Sum(c => (c.Product?.CurrentPrice ?? 0) * c.Quantity);

    public async Task<IActionResult> OnGetAsync()
    {
        string buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await LoadCartAsync(buyerId);
        return CartItems.Any() ? Page() : RedirectToPage("/Cart/Index");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await LoadCartAsync(buyerId);

        if (!CartItems.Any())
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty.");
            return Page();
        }

        foreach (CartItem item in CartItems)
        {
            if (item.Product == null || item.Product.Stock < item.Quantity)
            {
                ModelState.AddModelError(string.Empty,
                    $"'{item.Product?.Name}' does not have enough stock (Only {item.Product?.Stock} left).");
                return Page();
            }
        }

        ModelState.Remove("Order.BuyerId");
        ModelState.Remove("Order.PaymentStatus");
        ValidateDemoPayment();
        if (!ModelState.IsValid) return Page();

        Order.BuyerId = buyerId;
        Order.OrderDate = DateTime.Now;
        Order.Status = OrderStatus.Processing;
        Order.PaymentStatus = "Completed";
        Order.TotalAmount = TotalAmount;

        foreach (CartItem cartItem in CartItems)
        {
            Order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.Product!.CurrentPrice
            });
        }

        try
        {
            Order.PayPalOrderId = $"DEMO-{PaymentMethod.ToUpperInvariant()}-{Guid.NewGuid():N}";
            _context.Orders.Add(Order);
            foreach (CartItem item in CartItems) item.Product!.Stock -= item.Quantity;
            _context.CartItems.RemoveRange(CartItems);
            await _context.SaveChangesAsync();

            string displayedMethod = PaymentMethod == "DemoPay" ? "Quick Demo Pay" : PaymentMethod;
            return RedirectToPage("/Checkout/Success",
                new { orderId = Order.Id, method = PaymentMethod });
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty,
                "The demo payment could not be completed. Please try again.");
            return Page();
        }
    }

    private async Task LoadCartAsync(string buyerId)
    {
        CartItems = await _context.CartItems.Include(c => c.Product)
            .Where(c => c.BuyerId == buyerId).ToListAsync();
    }

    private void ValidateDemoPayment()
    {
        string[] allowedMethods = ["Card", "FPX", "EWallet","DemoPay"];
        if (!allowedMethods.Contains(PaymentMethod))
        {
            ModelState.AddModelError(nameof(PaymentMethod), "Please select a valid demo payment method.");
            return;
        }

        if (PaymentMethod == "Card")
        {
            string digits = new((CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length != 16)
                ModelState.AddModelError(nameof(CardNumber), "Enter a 16-digit demo card number.");
            if (string.IsNullOrWhiteSpace(CardHolder))
                ModelState.AddModelError(nameof(CardHolder), "Enter the demo cardholder name.");
            if (string.IsNullOrWhiteSpace(CardExpiry))
                ModelState.AddModelError(nameof(CardExpiry), "Enter the expiry date.");
            if (string.IsNullOrWhiteSpace(CardCvv) || CardCvv.Length != 3 || !CardCvv.All(char.IsDigit))
                ModelState.AddModelError(nameof(CardCvv), "Enter a 3-digit demo CVV.");
        }
        else if (PaymentMethod == "FPX" && string.IsNullOrWhiteSpace(BankName))
            ModelState.AddModelError(nameof(BankName), "Please select a demo bank.");
        else if (PaymentMethod == "EWallet" && string.IsNullOrWhiteSpace(EWalletName))
            ModelState.AddModelError(nameof(EWalletName), "Please select a demo eWallet.");
    }
}
