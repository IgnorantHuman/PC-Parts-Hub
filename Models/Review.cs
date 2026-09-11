using System.ComponentModel.DataAnnotations;

namespace PCPartsHub.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Required]
    public string BuyerId { get; set; } = string.Empty;
    public ApplicationUser? Buyer { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
