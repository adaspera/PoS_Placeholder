using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;

public class ProductVariationRepository : Repository<ProductVariation>
{
    public ProductVariationRepository(ApplicationDbContext db) : base(db) {}

    public async Task<IEnumerable<ProductVariation>> GetByProductAndBusinessId(int productId, int userBusinessId)
    {
        return await _db.ProductVariations
            .Where(pv => pv.ProductId == productId && pv.Product.BusinessId == userBusinessId)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<ProductVariation>> GetByVariationIdsAndBusinessIdAsync(IEnumerable<int> variationIds, int userBusinessId)
    {
        return await _db.ProductVariations
            .Where(pv => variationIds.Contains(pv.Id) && pv.Product.BusinessId == userBusinessId)
            .ToListAsync();
    }

}