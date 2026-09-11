using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PCPartsHub.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    public DateTime RegisteredDate { get; set; } = DateTime.Now;
}