using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;
using System.ComponentModel.DataAnnotations;

namespace PCShop.Pages.Admin.Products;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Product Product { get; set; } = default!;

    [BindProperty]
    [Required(ErrorMessage = "Rejection reason is required.")]
    [StringLength(
        500,
        MinimumLength = 1,
        ErrorMessage =
            "Reason must contain between 1 and 500 characters.")]
    [Display(Name = "Rejection Reason")]
    public string RejectReason { get; set; } = string.Empty;

    public bool ShowRejectForm { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Product? product = await GetProductAsync(id.Value);

        if (product == null)
        {
            return NotFound();
        }

        Product = product;

        RejectReason =
            product.RejectionReason ?? string.Empty;

        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        Product? product = await _context.Products
            .FirstOrDefaultAsync(product =>
                product.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        if (product.Status != ProductStatus.Pending)
        {
            TempData["ErrorMessage"] =
                "This listing has already been reviewed.";

            return RedirectToPage(
                "./Details",
                new { id });
        }

        ApplicationUser? admin =
            await _userManager.GetUserAsync(User);

        if (admin == null)
        {
            return Forbid();
        }

        product.Status = ProductStatus.Active;
        product.RejectionReason = null;
        product.ReviewedAt = DateTime.Now;

        AdminAction adminAction = new()
        {
            AdminId = admin.Id,
            ProductId = product.Id,
            Action = "Approved",
            Reason = null,
            ActionDate = DateTime.Now
        };

        _context.AdminActions.Add(adminAction);

        IList<ApplicationUser> buyers =
            await _userManager.GetUsersInRoleAsync("Buyer");

        foreach (ApplicationUser buyer in buyers)
        {
            Notification notification = new()
            {
                UserId = buyer.Id,
                Title = "New product available",
                Message =
                    $"{product.Name} has been approved and is now available.",
                Link = $"/Products/Details/{product.Id}",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"{product.Name} was approved successfully.";

        return RedirectToPage(
            "./Index",
            new { StatusFilter = "Active" });
    }

    public async Task<IActionResult> OnPostRejectAsync(int id)
    {
        Product? product = await GetProductAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        Product = product;
        ShowRejectForm = true;

        RejectReason =
            RejectReason?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(RejectReason))
        {
            ModelState.AddModelError(
                nameof(RejectReason),
                "Rejection reason cannot be empty.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Pending and Active products can be rejected.
        // A rejected product cannot be rejected again.
        if (product.Status == ProductStatus.Rejected)
        {
            TempData["ErrorMessage"] =
                "This listing is already rejected.";

            return RedirectToPage(
                "./Details",
                new { id });
        }

        ApplicationUser? admin =
            await _userManager.GetUserAsync(User);

        if (admin == null)
        {
            return Forbid();
        }

        product.Status = ProductStatus.Rejected;
        product.RejectionReason = RejectReason;
        product.ReviewedAt = DateTime.Now;

        // Remove the rejected product from existing carts.
        List<CartItem> affectedCartItems =
            await _context.CartItems
                .Where(item =>
                    item.ProductId == product.Id)
                .ToListAsync();

        _context.CartItems.RemoveRange(
            affectedCartItems);

        // Save the Admin action history.
        AdminAction adminAction = new()
        {
            AdminId = admin.Id,
            ProductId = product.Id,
            Action = "Rejected",
            Reason = RejectReason,
            ActionDate = DateTime.Now
        };

        _context.AdminActions.Add(adminAction);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"{product.Name} was rejected and removed " +
            "from the marketplace.";

        return RedirectToPage(
            "./Index",
            new { StatusFilter = "Rejected" });
    }

    private async Task<Product?> GetProductAsync(int id)
    {
        return await _context.Products
            .Include(product => product.Seller)
            .Include(product =>
                product.ProductImages)
            .FirstOrDefaultAsync(product =>
                product.Id == id);
    }
}