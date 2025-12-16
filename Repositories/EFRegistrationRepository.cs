using Microsoft.EntityFrameworkCore;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Repositories
{
    public class EFRegistrationRepository : IRegistrationRepository
    {
        private readonly ApplicationDbContext _context;

        public EFRegistrationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRegistrationAsync(Registration registration)
        {
            if (!string.IsNullOrEmpty(registration.UserId))
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == registration.UserId);
                if (!userExists)
                {
                    throw new InvalidOperationException($"UserId {registration.UserId} không tồn tại trong AspNetUsers.");
                }
            }

            var eventExists = await _context.Events.AnyAsync(e => e.Id == registration.EventId);
            if (!eventExists)
            {
                throw new InvalidOperationException($"EventId {registration.EventId} không tồn tại trong Events.");
            }

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Registration>> GetRegistrationsByEventAsync(int eventId)
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Where(r => r.EventId == eventId)
                .ToListAsync();
        }

        public async Task<List<Registration>> GetRegistrationsByUserAsync(string userId)
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<Registration> GetRegistrationByIdAsync(int registrationId)
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == registrationId);
        }

        public async Task UpdateRegistrationAsync(Registration registration)
        {
            _context.Registrations.Update(registration);
            await _context.SaveChangesAsync();
        }

        public async Task CancelRegistrationAsync(int registrationId, string? cancellationReason)
        {
            var registration = await _context.Registrations.FindAsync(registrationId);
            if (registration != null)
            {
                registration.CancellationReason = cancellationReason ?? "Không cung cấp lý do";
                registration.CancellationDate = DateTime.Now;
                _context.Registrations.Update(registration);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Registration>> GetRegistrationHistoryAsync(string userId)
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();
        }
    }
}