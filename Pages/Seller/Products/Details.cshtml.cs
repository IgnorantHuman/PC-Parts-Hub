using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Seller.Products;

[Authorize(Roles = "Seller")]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Product Product { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        string? sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Product? product = await _context.Products
            .AsNoTracking()
            .Include(item => item.ProductImages)
            .FirstOrDefaultAsync(item => item.Id == id && item.SellerId == sellerId);

        if (product == null)
        {
            return NotFound();
        }

        Product = product;
        return Page();
    }
}
