using QuickEvent.Models;

namespace QuickEvent.Repositories.Interfaces
{
    public interface IRegistrationRepository
    {
        Task AddRegistrationAsync(Registration registration);
        Task<List<Registration>> GetRegistrationsByEventAsync(int eventId);
        Task<List<Registration>> GetRegistrationsByUserAsync(string userId);
        Task<Registration> GetRegistrationByIdAsync(int registrationId);
        Task UpdateRegistrationAsync(Registration registration);
        Task CancelRegistrationAsync(int registrationId, string? cancellationReason);
        Task<List<Registration>> GetRegistrationHistoryAsync(string userId);
    }
}