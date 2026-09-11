using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Seller.Products;

[Authorize(Roles = "Seller")]
public class DeleteModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public DeleteModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public Product Product { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Product? product = await FindOwnedProductAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        if (product.Status != ProductStatus.Pending)
        {
            TempData["ErrorMessage"] = "Only a Pending listing can be deleted. Edit the listing or contact an Admin.";
            return RedirectToPage("./Index");
        }

        if (await _context.AdminActions.AnyAsync(action => action.ProductId == product.Id))
        {
            TempData["ErrorMessage"] = "This listing has Admin history and cannot be deleted. You may edit it instead.";
            return RedirectToPage("./Index");
        }

        Product = product;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Product? product = await FindOwnedProductAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        if (product.Status != ProductStatus.Pending ||
            await _context.AdminActions.AnyAsync(action => action.ProductId == product.Id))
        {
            TempData["ErrorMessage"] = "This listing cannot be deleted because it was reviewed or is no longer Pending.";
            return RedirectToPage("./Index");
        }

        List<string> imagePaths = product.ProductImages.Select(image => image.ImagePath).ToList();
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        foreach (string path in imagePaths)
        {
            DeletePhysicalImage(path);
        }

        TempData["SuccessMessage"] = $"{product.Name} was deleted.";
        return RedirectToPage("./Index");
    }

    private async Task<Product?> FindOwnedProductAsync(int? id)
    {
        if (id == null)
        {
            return null;
        }

        string? sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return await _context.Products
            .Include(product => product.ProductImages)
            .FirstOrDefaultAsync(product => product.Id == id && product.SellerId == sellerId);
    }

    private void DeletePhysicalImage(string relativePath)
    {
        string physicalPath = Path.Combine(
            _environment.WebRootPath,
            relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }
}
