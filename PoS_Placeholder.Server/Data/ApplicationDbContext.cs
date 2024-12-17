using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Add all models
    public DbSet<User> Users { get; set; }
    public DbSet<Business> Businesses { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariation> ProductVariations { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<ProductArchive> ProductsArchive { get; set; }
    public DbSet<TaxArchive> TaxesArchive { get; set; }
    public DbSet<DiscountArchive> DiscountsArchives { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<UserWorkTime> UserWorkTimes { get; set; }
    public DbSet<Giftcard> Giftcards { get; set; }
    public DbSet<PaymentArchive> PaymentsArchive  { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<ServiceArchive> ServicesArchive { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Business)
            .WithMany(b => b.Orders)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Products)
            .WithOne(b => b.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Taxes)
            .WithOne(b => b.Order)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Discounts)
            .WithOne(b => b.Order)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Payments)
            .WithOne(b => b.Order)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Order>()
            .HasMany(o => o.Services)
            .WithOne(b => b.Order)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Appointment>()
            .HasOne(o => o.User)
            .WithMany(b => b.Appointments)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Service>()
            .HasOne(o => o.User)
            .WithMany(o => o.Services)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Service>()
            .HasOne(o => o.Business)
            .WithMany(o => o.Services)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ServiceArchive>()
            .HasOne(o => o.Order)
            .WithMany(o => o.Services)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductArchive>()
            .HasOne(o => o.Order)
            .WithMany(o => o.Products)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TaxArchive>()
            .HasOne(o => o.Order)
            .WithMany(o => o.Taxes)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DiscountArchive>()
            .HasOne(o => o.Order)
            .WithMany(o => o.Discounts)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentArchive>()
            .HasOne(o => o.Order)
            .WithMany(o => o.Payments)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// dotnet ef migrations add "migrationName"
// dotnet ef database update
