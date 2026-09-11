using System.Security.Claims;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Data;
using PCPartsHub.Models;

namespace PCShop.Pages.Seller.Products;

[Authorize(Roles = "Seller")]
public class EditModel : PageModel
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/webp" };
    private const long MaximumImageSize = 5 * 1024 * 1024;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly HtmlSanitizer _htmlSanitizer = new();

    public EditModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public Product Product { get; set; } = default!;

    [BindProperty]
    public List<IFormFile> UploadImages { get; set; } = new();

    public List<ProductImage> ExistingImages { get; set; } = new();
    public IEnumerable<SelectListItem> CategoryOptions => SellerProductOptions.Categories;
    public IEnumerable<SelectListItem> ConditionOptions => SellerProductOptions.Conditions;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Product? product = await FindOwnedProductAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        Product = product;
        ExistingImages = product.ProductImages.OrderBy(image => image.ProductImageId).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Product? productToUpdate = await FindOwnedProductAsync(Product.Id);
        if (productToUpdate == null)
        {
            return NotFound();
        }

        ModelState.Remove("Product.SellerId");
        ValidateNewImages(productToUpdate.ProductImages.Count);
        ValidatePromotion();

        if (!ModelState.IsValid)
        {
            Product.ImagePath = productToUpdate.ImagePath;
            Product.Status = productToUpdate.Status;
            Product.RejectionReason = productToUpdate.RejectionReason;
            ExistingImages = productToUpdate.ProductImages.OrderBy(image => image.ProductImageId).ToList();
            return Page();
        }

        productToUpdate.Name = Product.Name;
        productToUpdate.Brand = Product.Brand;
        productToUpdate.Category = Product.Category;
        productToUpdate.Condition = Product.Condition;
        productToUpdate.Description = _htmlSanitizer.Sanitize(Product.Description);
        productToUpdate.Price = Product.Price;
        productToUpdate.Stock = Product.Stock;
        productToUpdate.DiscountPercentage = Product.DiscountPercentage;
        productToUpdate.PromotionStart = Product.PromotionStart;
        productToUpdate.PromotionEnd = Product.PromotionEnd;

        foreach (IFormFile image in UploadImages.Where(image => image.Length > 0))
        {
            string relativePath = await SaveImageAsync(image);
            productToUpdate.ProductImages.Add(new ProductImage { ImagePath = relativePath });
            productToUpdate.ImagePath ??= relativePath;
        }

        // Any edit must be reviewed again before Buyers can see it.
        productToUpdate.Status = ProductStatus.Pending;
        productToUpdate.RejectionReason = null;
        productToUpdate.ReviewedAt = null;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{productToUpdate.Name} was updated and returned to Pending review.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(int imageId, int productId)
    {
        Product? product = await FindOwnedProductAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        ProductImage? image = product.ProductImages.FirstOrDefault(item => item.ProductImageId == imageId);
        if (image == null)
        {
            return NotFound();
        }

        if (product.ProductImages.Count <= 1)
        {
            TempData["ErrorMessage"] = "A product must keep at least one image.";
            return RedirectToPage("./Edit", new { id = productId });
        }

        string deletedPath = image.ImagePath;
        _context.ProductImages.Remove(image);

        if (product.ImagePath == deletedPath)
        {
            product.ImagePath = product.ProductImages
                .Where(item => item.ProductImageId != imageId)
                .OrderBy(item => item.ProductImageId)
                .Select(item => item.ImagePath)
                .FirstOrDefault();
        }

        product.Status = ProductStatus.Pending;
        product.RejectionReason = null;
        product.ReviewedAt = null;
        await _context.SaveChangesAsync();
        DeletePhysicalImage(deletedPath);

        TempData["SuccessMessage"] = "Product image was deleted. The listing returned to Pending review.";
        return RedirectToPage("./Edit", new { id = productId });
    }

    public async Task<IActionResult> OnPostSetCoverAsync(int imageId, int productId)
    {
        Product? product = await FindOwnedProductAsync(productId);
        ProductImage? image = product?.ProductImages.FirstOrDefault(item => item.ProductImageId == imageId);

        if (product == null || image == null)
        {
            return NotFound();
        }

        product.ImagePath = image.ImagePath;
        product.Status = ProductStatus.Pending;
        product.RejectionReason = null;
        product.ReviewedAt = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cover image changed. The listing returned to Pending review.";
        return RedirectToPage("./Edit", new { id = productId });
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

    private void ValidateNewImages(int existingCount)
    {
        List<IFormFile> images = UploadImages.Where(image => image.Length > 0).ToList();
        if (existingCount + images.Count > 5)
        {
            ModelState.AddModelError(nameof(UploadImages), "A product may have a maximum of five images.");
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

    private void ValidatePromotion()
    {
        bool hasDiscount =
            Product.DiscountPercentage.HasValue;

        bool hasStart =
            Product.PromotionStart.HasValue;

        bool hasEnd =
            Product.PromotionEnd.HasValue;

        // All empty means no promotion.
        if (!hasDiscount && !hasStart && !hasEnd)
        {
            return;
        }

        if (!hasDiscount)
        {
            ModelState.AddModelError(
                "Product.DiscountPercentage",
                "Enter a discount percentage.");
        }

        if (!hasStart)
        {
            ModelState.AddModelError(
                "Product.PromotionStart",
                "Select the promotion start date.");
        }

        if (!hasEnd)
        {
            ModelState.AddModelError(
                "Product.PromotionEnd",
                "Select the promotion end date.");
        }

        if (hasStart &&
            hasEnd &&
            Product.PromotionEnd <= Product.PromotionStart)
        {
            ModelState.AddModelError(
                "Product.PromotionEnd",
                "Promotion end must be later than its start.");
        }
    }
}
