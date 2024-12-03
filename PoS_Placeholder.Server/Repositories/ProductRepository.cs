using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;

public class ProductRepository : Repository<Product>
{
    public ProductRepository(ApplicationDbContext db) : base(db) {}
    
    public Product GetByIdAndBusiness(int id, int userBusinessId)
    {
        return _db.Products.FirstOrDefault(p => p.Id == id && p.BusinessId == userBusinessId);
    }
    
    public async Task<Product> GetByIdAndBusinessAsync(int id, int userBusinessId)
    {
        return await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.BusinessId == userBusinessId);
    }
}