using Microsoft.EntityFrameworkCore;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Repositories
{
    public class EFFormAccessRepository : IFormAccessRepository
    {
        private readonly ApplicationDbContext _context;

        public EFFormAccessRepository (ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddFormAccessAsync(FormAccess formAccess)
        {
            _context.FormAccesses.Add(formAccess);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetFormAccessCountAsync(int eventId)
        {
            return await _context.FormAccesses
                .CountAsync(f => f.EventId == eventId);
        }
    }
}