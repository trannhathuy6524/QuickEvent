using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Data;

namespace QuickEvent.Repositories.Interfaces
{
    public interface IEventRepository
    {
        Task<List<Event>> GetEventsByOrganizerAsync(string organizerId);
        Task<Event> GetEventByIdAsync(int id);
        Task AddEventAsync(Event @event);
        Task UpdateEventAsync(Event @event);
        Task CancelEventAsync(int id);
        Task DeleteEventAsync(int id);
        Task<List<Event>> SearchEventsAsync(string query, bool isPublicOnly);
        Task<int> GetRegistrationCountAsync(int eventId);
        Task UpdateEventStatusAsync(int eventId);
        Task<Dictionary<DateTime, int>> GetRegistrationsByDayAsync(int eventId, DateTime startDate, DateTime endDate);
        Task<Dictionary<DateTime, int>> GetRegistrationsByWeekAsync(int eventId, DateTime startDate, DateTime endDate);
    }
}