using Microsoft.EntityFrameworkCore;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Repositories
{
    public class EFOrganizerRequestRepository : IOrganizerRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public EFOrganizerRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRequestAsync(OrganizerRequest request)
        {
            _context.OrganizerRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task<List<OrganizerRequest>> GetPendingRequestsAsync()
        {
            return await _context.OrganizerRequests
                .Include(r => r.User) // Thêm Include User để lấy thông tin người dùng
                .Where(r => r.Status == "Chờ phê duyệt")
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task<OrganizerRequest> GetRequestByIdAsync(int id)
        {
            return await _context.OrganizerRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task UpdateRequestAsync(OrganizerRequest request)
        {
            _context.OrganizerRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task<OrganizerRequest> GetPendingRequestByUserIdAsync(string userId)
        {
            return await _context.OrganizerRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Chờ phê duyệt");
        }
    }
}