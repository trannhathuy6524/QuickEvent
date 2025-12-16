using QuickEvent.Models;

namespace QuickEvent.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(Notification notification);
        Task<List<Notification>> GetNotificationsByUserAsync(string userId);
        Task MarkAsReadAsync(int notificationId);
    }
}