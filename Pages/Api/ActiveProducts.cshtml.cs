using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Api;

public class ActiveProductsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ActiveProductsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        int count = await _context.Products
            .AsNoTracking()
            .CountAsync(product => product.Status == ProductStatus.Active);

        Response.Headers["Cache-Control"] = "no-store, no-cache";

        return new JsonResult(new
        {
            success = true,
            count,
            updatedAt = DateTimeOffset.UtcNow
        });
    }
}
