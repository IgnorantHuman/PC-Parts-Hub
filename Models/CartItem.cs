using System.ComponentModel.DataAnnotations;
using PCPartsHub.Models;

namespace PCPartsHub.Models;

public class CartItem
{
    public int Id { get; set; }

    [Required]
    public string BuyerId { get; set; } = string.Empty;
    public ApplicationUser? Buyer { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Range(1, 999, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 1;

    public DateTime AddedAt { get; set; } = DateTime.Now;
}