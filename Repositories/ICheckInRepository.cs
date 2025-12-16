using QuickEvent.Models;

namespace QuickEvent.Repositories
{
    public interface ICheckInRepository
    {
        Task AddCheckInAsync(CheckIn checkIn);
        Task<CheckIn> GetCheckInByRegistrationId(int registrationId);
        Task<List<CheckIn>> GetCheckInsByEventAsync(int eventId);
    }
}