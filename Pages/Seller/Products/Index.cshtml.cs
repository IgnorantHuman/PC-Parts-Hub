using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Seller.Products;

[Authorize(Roles = "Seller")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Product> ProductList { get; set; } = new List<Product>();

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; } = "All";

    public async Task OnGetAsync()
    {
        string? sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        IQueryable<Product> query = _context.Products
            .AsNoTracking()
            .Where(product => product.SellerId == sellerId);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchTerm = SearchTerm.Trim();
            query = query.Where(product =>
                product.Name.Contains(SearchTerm) ||
                product.Brand.Contains(SearchTerm) ||
                product.Category.Contains(SearchTerm));
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter) &&
            !StatusFilter.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse(StatusFilter, true, out ProductStatus selectedStatus))
        {
            query = query.Where(product => product.Status == selectedStatus);
        }

        ProductList = await query
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync();
    }
}
