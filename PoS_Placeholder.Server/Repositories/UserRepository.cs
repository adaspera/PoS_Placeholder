using Microsoft.AspNetCore.Identity;
using PoS_Placeholder.Server.Models;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Repositories
{
    public class UserRepository : Repository<User>
    {
        private readonly UserManager<User> _userManager;
        public UserRepository(ApplicationDbContext db, UserManager<User> userManager) : base(db)
        {
            _userManager = userManager;
        }

        public async Task<User?> GetEmployeeByIdAndBusinessAsync(string employeeId, int businessId)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId && u.BusinessId == businessId);
        }

        public async Task<IEnumerable<User>> GetUsersByBusinessIdAsync(int businessId)
        {
            return await _db.Users.Where(u => u.BusinessId == businessId).ToListAsync();
        }
        
    }
}
