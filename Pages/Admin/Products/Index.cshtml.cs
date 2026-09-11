using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Admin.Products;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Product> ProductList { get; set; }
        = new List<Product>();

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string StatusFilter { get; set; } = "Pending";

    public async Task OnGetAsync()
    {
        IQueryable<Product> productQuery = _context.Products
            .Include(product => product.Seller)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchTerm = SearchTerm.Trim();

            productQuery = productQuery.Where(product =>
                product.Name.Contains(SearchTerm) ||
                product.Brand.Contains(SearchTerm) ||
                product.Category.Contains(SearchTerm) ||
                (
                    product.Seller != null &&
                    (
                        product.Seller.FullName.Contains(SearchTerm) ||
                        (
                            product.Seller.Email != null &&
                            product.Seller.Email.Contains(SearchTerm)
                        )
                    )
                ));
        }

        if (string.IsNullOrWhiteSpace(StatusFilter))
        {
            StatusFilter = "Pending";
        }

        if (!StatusFilter.Equals(
                "All",
                StringComparison.OrdinalIgnoreCase))
        {
            bool validStatus = Enum.TryParse(
                StatusFilter,
                true,
                out ProductStatus selectedStatus);

            if (!validStatus)
            {
                selectedStatus = ProductStatus.Pending;
                StatusFilter = "Pending";
            }

            productQuery = productQuery.Where(product =>
                product.Status == selectedStatus);
        }

        ProductList = await productQuery
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync();
    }
}