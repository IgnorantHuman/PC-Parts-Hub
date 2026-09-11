using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCPartsHub.Models;

public enum ProductStatus
{
    Pending,
    Active,
    Rejected
}

public class Product
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Product Name")]
    public string Name { get; set; }
        = string.Empty;

    [Required]
    [StringLength(50)]
    public string Brand { get; set; }
        = string.Empty;

    [Required]
    [StringLength(50)]
    public string Category { get; set; }
        = string.Empty;

    [Required]
    [StringLength(
        2000,
        MinimumLength = 10,
        ErrorMessage =
            "Description must contain between 10 and 2000 characters.")]
    public string Description { get; set; }
        = string.Empty;

    [Required]
    [Range(
        0.01,
        999999.99,
        ErrorMessage =
            "Price must be between RM0.01 and RM999,999.99.")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Range(
    1,
    90,
    ErrorMessage = "Discount must be between 1% and 90%.")]
    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Discount Percentage")]
    public decimal? DiscountPercentage { get; set; }

    [Display(Name = "Promotion Start")]
    public DateTime? PromotionStart { get; set; }

    [Display(Name = "Promotion End")]
    public DateTime? PromotionEnd { get; set; }

    [NotMapped]
    public bool IsPromotionActive
    {
        get
        {
            if (!DiscountPercentage.HasValue ||
                !PromotionStart.HasValue ||
                !PromotionEnd.HasValue)
            {
                return false;
            }

            // Malaysia time is UTC+8.
            DateTime now = DateTime.UtcNow.AddHours(8);

            return DiscountPercentage.Value > 0 &&
                   PromotionStart.Value <= now &&
                   PromotionEnd.Value >= now;
        }
    }

    [NotMapped]
    public decimal CurrentPrice
    {
        get
        {
            return IsPromotionActive
                ? PromotionPrice
                : Price;
        }
    }

    [NotMapped]
    public decimal PromotionPrice
    {
        get
        {
            if (!DiscountPercentage.HasValue)
            {
                return Price;
            }

            decimal discount =
                Price * DiscountPercentage.Value / 100;

            return decimal.Round(
                Price - discount,
                2,
                MidpointRounding.AwayFromZero);
        }
    }

    [Required]
    [Range(
        0,
        9999,
        ErrorMessage =
            "Stock must be between 0 and 9999.")]
    public int Stock { get; set; }

    [Required]
    [StringLength(30)]
    public string Condition { get; set; }
        = "New";

    [StringLength(300)]
    [Display(Name = "Product Image")]
    public string? ImagePath { get; set; }

    // Foreign key connecting the product to its Seller.
    [Required]
    public string SellerId { get; set; }
        = string.Empty;

    public ApplicationUser? Seller { get; set; }

    public ProductStatus Status { get; set; }
        = ProductStatus.Pending;

    [StringLength(
        500,
        ErrorMessage =
            "The rejection reason cannot exceed 500 characters.")]
    [Display(Name = "Rejection Reason")]
    public string? RejectionReason { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedAt { get; set; }
        = DateTime.Now;

    [Display(Name = "Reviewed Date")]
    public DateTime? ReviewedAt { get; set; }

    // A Seller may upload several pictures for one listing.
    public ICollection<ProductImage> ProductImages { get; set; }
        = new List<ProductImage>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
