using PoS_Placeholder.Server.Models;
using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;


namespace PoS_Placeholder.Server.Repositories
{
    public class UserWorkTimeRepository : Repository<UserWorkTime>
    {
        public UserWorkTimeRepository(ApplicationDbContext db) : base(db) { }

        public async Task<UserWorkTime> GetScheduleByIdAndEmployeeAsync(string employeeId, int scheduleId)
        {
            return await _db.UserWorkTimes.FirstOrDefaultAsync(u => u.Id == scheduleId && u.UserId == employeeId);
        }
        public async Task<IEnumerable<UserWorkTime>> GetSchedulesByEmployeeIdAsync(string employeeId)
        {
            return await _db.UserWorkTimes.Where(wt => wt.UserId == employeeId).ToListAsync();
        }

    }
}
