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
}

// dotnet ef migrations add "migrationName"
// dotnet ef database update
