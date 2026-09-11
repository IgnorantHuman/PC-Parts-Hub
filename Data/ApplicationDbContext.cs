using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PCPartsHub.Models;

namespace PCPartsHub.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<AdminAction> AdminActions => Set<AdminAction>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>()
            .HasOne(product => product.Seller)
            .WithMany()
            .HasForeignKey(product => product.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .Property(product => product.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Entity<ProductImage>()
            .HasOne(image => image.Product)
            .WithMany(product => product.ProductImages)
            .HasForeignKey(image => image.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AdminAction>()
            .HasOne(adminAction => adminAction.Admin)
            .WithMany()
            .HasForeignKey(adminAction => adminAction.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AdminAction>()
            .HasOne(adminAction => adminAction.Product)
            .WithMany()
            .HasForeignKey(adminAction => adminAction.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Buyer entity configurations
        builder.Entity<CartItem>()
            .HasOne(c => c.Buyer)
            .WithMany()
            .HasForeignKey(c => c.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CartItem>()
            .HasOne(c => c.Product)
            .WithMany()
            .HasForeignKey(c => c.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Order>()
            .HasOne(o => o.Buyer)
            .WithMany()
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(review => review.Product)
            .WithMany(product => product.Reviews)
            .HasForeignKey(review => review.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(review => review.Buyer)
            .WithMany()
            .HasForeignKey(review => review.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasIndex(review => new { review.ProductId, review.BuyerId })
            .IsUnique();

        builder.Entity<Notification>()
            .HasOne(notification => notification.User)
            .WithMany()
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Notification>()
            .HasIndex(notification => new
            {
                notification.UserId,
                notification.IsRead
            });
    }
}
