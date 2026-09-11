using System.ComponentModel.DataAnnotations;

namespace PCPartsHub.Models;

public class ProductImage
{
    public int ProductImageId { get; set; }

    [Required]
    [StringLength(300)]
    public string ImagePath { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public Product? Product { get; set; }
}
