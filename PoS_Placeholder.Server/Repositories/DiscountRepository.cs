using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;

public class DiscountRepository : Repository<Discount>
{
    public DiscountRepository(ApplicationDbContext db) : base(db) {}
    
    public async Task<Discount?> GetDiscountByDiscountAndBusinessId(int discountId, int userBusinessId)
    {
        return _db.Discounts
            .FirstOrDefault(d => d.Id == discountId && d.BusinessId == userBusinessId);
    }
    
    
}