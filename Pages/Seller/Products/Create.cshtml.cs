using System.Security.Claims;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Seller.Products;

[Authorize(Roles = "Seller")]
public class CreateModel : PageModel
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/webp" };
    private const long MaximumImageSize = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly HtmlSanitizer _htmlSanitizer = new();

    public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public Product Product { get; set; } = new();

    [BindProperty]
    public List<IFormFile> UploadImages { get; set; } = new();

    public IEnumerable<SelectListItem> CategoryOptions => SellerProductOptions.Categories;
    public IEnumerable<SelectListItem> ConditionOptions => SellerProductOptions.Conditions;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string? sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sellerId == null)
        {
            return Challenge();
        }

        Product.SellerId = sellerId;
        Product.Status = ProductStatus.Pending;
        Product.RejectionReason = null;
        Product.ReviewedAt = null;
        Product.CreatedAt = DateTime.Now;

        // SellerId is assigned by the server rather than posted by the browser.
        ModelState.Remove("Product.SellerId");

        ValidateImages(requireAtLeastOne: true);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Product.Description = _htmlSanitizer.Sanitize(Product.Description);

        foreach (IFormFile image in UploadImages.Where(image => image.Length > 0))
        {
            string relativePath = await SaveImageAsync(image);
            Product.ProductImages.Add(new ProductImage { ImagePath = relativePath });
            Product.ImagePath ??= relativePath;
        }

        _context.Products.Add(Product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{Product.Name} was submitted for Admin review.";
        return RedirectToPage("./Index");
    }

    private void ValidateImages(bool requireAtLeastOne)
    {
        List<IFormFile> images = UploadImages.Where(image => image.Length > 0).ToList();

        if (requireAtLeastOne && images.Count == 0)
        {
            ModelState.AddModelError(nameof(UploadImages), "Please upload at least one product image.");
        }

        if (images.Count > 5)
        {
            ModelState.AddModelError(nameof(UploadImages), "You may upload a maximum of five images.");
        }

        foreach (IFormFile image in images)
        {
            string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(UploadImages), "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            if (!AllowedContentTypes.Contains(image.ContentType.ToLowerInvariant()))
            {
                ModelState.AddModelError(nameof(UploadImages), "The selected file is not a supported image.");
            }

            if (image.Length > MaximumImageSize)
            {
                ModelState.AddModelError(nameof(UploadImages), "Each image must be 5 MB or smaller.");
            }
        }
    }

    private async Task<string> SaveImageAsync(IFormFile image)
    {
        string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        string fileName = $"{Guid.NewGuid():N}{extension}";
        string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadFolder);

        string filePath = Path.Combine(uploadFolder, fileName);
        await using FileStream stream = new(filePath, FileMode.CreateNew);
        await image.CopyToAsync(stream);

        return $"/uploads/{fileName}";
    }
}

internal static class SellerProductOptions
{
    public static readonly List<SelectListItem> Categories = new()
    {
        new("CPU", "CPU"), new("GPU", "GPU"), new("Motherboard", "Motherboard"),
        new("RAM", "RAM"), new("Storage", "Storage"), new("Power Supply", "Power Supply"),
        new("CPU Cooler", "CPU Cooler"), new("PC Case", "PC Case"),
        new("Monitor", "Monitor"), new("Accessories", "Accessories")
    };

    public static readonly List<SelectListItem> Conditions = new()
    {
        new("New", "New"), new("Like New", "Like New"),
        new("Used - Good", "Used - Good"), new("Used - Fair", "Used - Fair")
    };
}
