using Microsoft.EntityFrameworkCore;
using PoS_Placeholder.Server.Data;
using PoS_Placeholder.Server.Models;

namespace PoS_Placeholder.Server.Repositories;

public class AppointmentRepository : Repository<Appointment>
{
    public AppointmentRepository(ApplicationDbContext db) : base(db) { }

    public async Task<Appointment> GetAppointmentByIdAsync(int id, int businessId)
    {
        return await _db.Appointments
            .FirstOrDefaultAsync(o => o.BusinessId == businessId && o.Id == id);
    }
    public async Task<IEnumerable<Appointment>> GetAppointmentsByUserIdAsync(int businessId, string userId)
    {
        return await _db.Appointments
            .Where(o => o.BusinessId == businessId && o.UserId == userId).ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByBusinessIdAsync(int businessId)
    {
        return await _db.Appointments
            .Where(o => o.BusinessId == businessId).ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByBusinessIdAsync(int businessId, DateTime startDate)
    {
        return await _db.Appointments
            .Where(o => o.BusinessId == businessId && DateTime.Parse(o.TimeReserved) > startDate).ToListAsync();
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsByBusinessIdAsync(int businessId, DateTime? startDate, DateTime endDate)
    {
        if(startDate == null)
        {
            return await _db.Appointments
                .Where(o => o.BusinessId == businessId && DateTime.Parse(o.TimeReserved) < endDate).ToListAsync();
        }
        else
        {
            return await _db.Appointments
                .Where(o => o.BusinessId == businessId && DateTime.Parse(o.TimeReserved) >= startDate && DateTime.Parse(o.TimeReserved) < endDate).ToListAsync();
        }
    }
}