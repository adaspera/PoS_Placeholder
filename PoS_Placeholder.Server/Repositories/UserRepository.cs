using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;

public class UserRepository : Repository<User>
{
    public UserRepository(ApplicationDbContext db) : base(db) { }

}