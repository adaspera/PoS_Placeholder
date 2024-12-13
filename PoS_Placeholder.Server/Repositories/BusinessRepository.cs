using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;
public class BusinessRepository : Repository<Business>
{
    public BusinessRepository(ApplicationDbContext db) : base(db) { }

}

