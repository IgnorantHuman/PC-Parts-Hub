using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Product> Products { get; set; } = new();

    public List<string> Categories { get; set; } = new();

    public async Task OnGetAsync()
    {
        // Only Admin-approved products appear on the Home page.
        Products = await _context.Products
            .AsNoTracking()
            .Include(product => product.Seller)
            .Include(product => product.ProductImages)
            .Where(product =>
                product.Status == ProductStatus.Active)
            .OrderByDescending(product => product.CreatedAt)
            .Take(12)
            .ToListAsync();

        Categories = Products
            .Select(product => product.Category)
            .Distinct()
            .OrderBy(category => category)
            .ToList();
    }
}