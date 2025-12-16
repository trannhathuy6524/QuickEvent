using Microsoft.EntityFrameworkCore;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Repositories
{
    public class EFNotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public EFNotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetNotificationsByUserAsync(string userId)
        {
            return await _context.Notifications
                .Include(n => n.Registration)
                .ThenInclude(r => r.Event)
                .Include(n => n.User)
                .Include(n => n.Event)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
            }
        }
    }
}