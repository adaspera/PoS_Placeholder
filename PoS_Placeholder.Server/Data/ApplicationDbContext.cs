using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    // Add all models
    public DbSet<Product> Products { get; set; }
}

// dotnet ef migrations add "migrationName"
// dotnet ef database update
