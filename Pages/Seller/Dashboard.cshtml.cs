using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Seller;

[Authorize(Roles = "Seller")]
public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int PendingProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int RejectedProducts { get; set; }
    public List<string> ProductNames { get; set; } = new();
    public List<int> ProductStocks { get; set; } = new();
    public List<Product> RecentRejectedProducts { get; set; } = new();
    public int UnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public List<string> SalesProductNames { get; set; } = new();
    public List<int> SalesQuantities { get; set; } = new();

    public async Task OnGetAsync()
    {
        string? sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        List<Product> products = await _context.Products
            .AsNoTracking()
            .Where(product => product.SellerId == sellerId)
            .OrderByDescending(product => product.CreatedAt)
            .ToListAsync();

        TotalProducts = products.Count;
        LowStockProducts = products.Count(product =>
            product.Status == ProductStatus.Active &&
            product.Stock > 0 &&
            product.Stock < 5);
        PendingProducts = products.Count(product => product.Status == ProductStatus.Pending);
        ActiveProducts = products.Count(product => product.Status == ProductStatus.Active);
        RejectedProducts = products.Count(product => product.Status == ProductStatus.Rejected);
        ProductNames = products.Select(product => product.Name).ToList();
        ProductStocks = products.Select(product => product.Stock).ToList();
        RecentRejectedProducts = products
            .Where(product => product.Status == ProductStatus.Rejected)
            .OrderByDescending(product => product.ReviewedAt)
            .Take(3)
            .ToList();

        var sales = await _context.OrderItems.AsNoTracking()
            .Where(item => item.Product!.SellerId == sellerId &&
                item.Order!.PaymentStatus == "Completed")
            .GroupBy(item => new { item.ProductId, item.Product!.Name })
            .Select(group => new
            {
                group.Key.Name,
                Quantity = group.Sum(item => item.Quantity),
                Revenue = group.Sum(item => item.Quantity * item.UnitPrice)
            }).OrderByDescending(item => item.Quantity).ToListAsync();
        UnitsSold = sales.Sum(item => item.Quantity);
        TotalRevenue = sales.Sum(item => item.Revenue);
        SalesProductNames = sales.Select(item => item.Name).ToList();
        SalesQuantities = sales.Select(item => item.Quantity).ToList();

        var ratings = await _context.Reviews.AsNoTracking()
            .Where(review => review.Product!.SellerId == sellerId)
            .Select(review => review.Rating).ToListAsync();
        ReviewCount = ratings.Count;
        AverageRating = ratings.Count == 0 ? 0 : ratings.Average();
    }
}
