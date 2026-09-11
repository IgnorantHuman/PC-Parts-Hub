using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Products;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Product Product { get; set; } = default!;
    public bool CanReview { get; set; }
    public Review? ExistingReview { get; set; }

    [BindProperty]
    public ReviewInput ReviewForm { get; set; } = new();

    public class ReviewInput
    {
        [System.ComponentModel.DataAnnotations.Range(1, 5)]
        public int Rating { get; set; } = 5;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(500, MinimumLength = 3)]
        public string Comment { get; set; } = string.Empty;
    }

    [BindProperty]
    public int Quantity { get; set; } = 1;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Seller)
            .Include(p => p.ProductImages)
            .Include(p => p.Reviews)
            .ThenInclude(review => review.Buyer)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == ProductStatus.Active);

        if (product == null) return NotFound();

        Product = product;
        string buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        ExistingReview = product.Reviews.FirstOrDefault(review => review.BuyerId == buyerId);
        CanReview = ExistingReview == null && await _context.OrderItems.AnyAsync(item =>
            item.ProductId == product.Id && item.Order!.BuyerId == buyerId &&
            item.Order.PaymentStatus == "Completed");
        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null || product.Status != ProductStatus.Active)
        {
            return NotFound();
        }

        if (Quantity <= 0 || Quantity > product.Stock)
        {
            StatusMessage = "Invalid quantity or exceeds available stock.";
            return RedirectToPage(new { id });
        }

        var buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.BuyerId == buyerId && c.ProductId == id);

        if (existingItem != null)
        {
            int newTotal = existingItem.Quantity + Quantity;
            if (newTotal > product.Stock)
            {
                StatusMessage = $"Cannot add {Quantity} more. You already have {existingItem.Quantity} in cart (Stock: {product.Stock}).";
                return RedirectToPage(new { id });
            }
            existingItem.Quantity = newTotal;
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                BuyerId = buyerId,
                ProductId = id,
                Quantity = Quantity
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToPage("/Cart/Index");
    }

    public async Task<IActionResult> OnPostReviewAsync(int id)
    {
        string buyerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        bool purchased = await _context.OrderItems.AnyAsync(item =>
            item.ProductId == id && item.Order!.BuyerId == buyerId &&
            item.Order.PaymentStatus == "Completed");
        bool alreadyReviewed = await _context.Reviews.AnyAsync(review =>
            review.ProductId == id && review.BuyerId == buyerId);

        if (!purchased || alreadyReviewed)
        {
            StatusMessage = "Only verified buyers may review a product once.";
            return RedirectToPage(new { id });
        }

        if (!ModelState.IsValid)
        {
            StatusMessage = "Please select 1–5 stars and enter a comment of 3–500 characters.";
            return RedirectToPage(new { id });
        }

        _context.Reviews.Add(new Review
        {
            ProductId = id,
            BuyerId = buyerId,
            Rating = ReviewForm.Rating,
            Comment = ReviewForm.Comment.Trim()
        });
        await _context.SaveChangesAsync();
        StatusMessage = "Thank you. Your verified purchase review was published.";
        return RedirectToPage(new { id });
    }
}
