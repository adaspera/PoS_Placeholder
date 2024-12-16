using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;

public class ServiceRepository : Repository<Service>
{
    public ServiceRepository(ApplicationDbContext db) : base(db) { }

    public async Task<Service> GetServiceByIdAsync(int id, int businessId)
    {
        return await _db.Services
            .FirstOrDefaultAsync(o => o.Id == id && o.BusinessId == businessId);
    }
    public async Task<IEnumerable<Service>> GetServicesByUserIdAsync(int businessId, string userId)
    {
        return await _db.Services
            .Where(o => o.UserId == userId && o.BusinessId == businessId).ToListAsync();
    }

    public async Task<IEnumerable<Service>> GetServicesByUserIdAsync(int businessId, string userId, bool isPercentage)
    {
        return await _db.Services
            .Where(o => o.UserId == userId && o.BusinessId == businessId && o.IsPercentage == isPercentage).ToListAsync();
    }

    public async Task<IEnumerable<Service>> GetServicesByBusinessIdAsync(int businessId)
    {
        return await _db.Services
            .Where(o => o.BusinessId == businessId).ToListAsync();
    }

    public async Task<IEnumerable<Service>> GetServicesByBusinessIdAsync(int businessId, bool isPercentage)
    {
        return await _db.Services
            .Where(o => o.BusinessId == businessId && o.IsPercentage == isPercentage).ToListAsync();
    }

    public async Task<IEnumerable<Service>> GetServicesByBusinessIdAsync(int businessId, decimal priceMin, bool isPercentage)
    {
        return await _db.Services
            .Where(o => o.BusinessId == businessId && o.ServiceCharge > priceMin && o.IsPercentage == isPercentage).ToListAsync();
    }

    // allows retireval of services within given price range
    // lower limit can be null here and return values are adjusted accordingly (all services priced below priceMax)
    public async Task<IEnumerable<Service>> GetServicesByBusinessIdAsync(int businessId, decimal? priceMin, decimal priceMax, bool isPercentage)
    {
        if (priceMin == null)
        {
            return await _db.Services
                .Where(o => o.BusinessId == businessId && o.ServiceCharge < priceMax && o.IsPercentage == isPercentage).ToListAsync();
        }
        else
        {
            return await _db.Services
                .Where(o => o.BusinessId == businessId && o.ServiceCharge > priceMin && o.ServiceCharge < priceMax && o.IsPercentage == isPercentage).ToListAsync();
        }
    }
}