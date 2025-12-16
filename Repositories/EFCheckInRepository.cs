using Microsoft.EntityFrameworkCore;
using QuickEvent.Data;
using QuickEvent.Models;

namespace QuickEvent.Repositories
{
    public class EFCheckInRepository : ICheckInRepository
    {
        private readonly ApplicationDbContext _context;

        public EFCheckInRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddCheckInAsync(CheckIn checkIn)
        {
            _context.CheckIns.Add(checkIn);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CheckIn>> GetCheckInsByEventAsync(int eventId)
        {
            return await _context.CheckIns
                .Join(_context.Registrations,
                    c => c.RegistrationId,
                    r => r.Id,
                    (c, r) => new { CheckIn = c, Registration = r })
                .Where(x => x.CheckIn.EventId == eventId && x.Registration.EventId == eventId && x.Registration.CancellationDate == null)
                .Select(x => x.CheckIn)
                .ToListAsync();
        }

        public async Task<CheckIn> GetCheckInByRegistrationId(int registrationId)
        {
            return await _context.CheckIns
                .FirstOrDefaultAsync(c => c.RegistrationId == registrationId);
        }
    }
}
