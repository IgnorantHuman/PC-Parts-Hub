using System.ComponentModel.DataAnnotations;

namespace PCPartsHub.Models;

public class AdminAction
{
    public int AdminActionId { get; set; }

    [Required]
    public string AdminId { get; set; } = string.Empty;

    [Required]
    public int ProductId { get; set; }

    [Required]
    [StringLength(20)]
    public string Action { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Reason { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.Now;

    public ApplicationUser? Admin { get; set; }

    public Product? Product { get; set; }
}