using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PCPartsHub.Models;

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Completed,
    Cancelled
}

public class Order
{
    public int Id { get; set; }

    [Required]
    public string BuyerId { get; set; } = string.Empty;
    public ApplicationUser? Buyer { get; set; }

    [Display(Name = "Order Date")]
    public DateTime OrderDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required]
    [StringLength(20)]
    public string PaymentStatus { get; set; } = "Pending";

    [StringLength(100)]
    public string? PayPalOrderId { get; set; }

    [StringLength(100)]
    public string? PayPalCaptureId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Recipient Name")]
    public string RecipientName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    [Display(Name = "Shipping Address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [Display(Name = "Postal Code")]
    public string PostalCode { get; set; } = string.Empty;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
