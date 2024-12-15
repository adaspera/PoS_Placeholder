using PoS_Placeholder.Server.Models;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;


namespace PoS_Placeholder.Server.Repositories;
public class BusinessRepository : Repository<Business>
{
    public BusinessRepository(ApplicationDbContext db) : base(db) { }
    public async Task<bool> UniquePhoneOrEmailAsync(string phone, string email, int business_id)
    {
        return await _db.Businesses.AnyAsync(b => (b.Phone == phone || b.Email == email) && b.Id != business_id);
    }

    public async Task<bool> ExistsByPhoneOrEmailAsync(string phone, string email)
    {
        return await _db.Businesses.AnyAsync(b => (b.Phone == phone || b.Email == email));
    }

    
}
