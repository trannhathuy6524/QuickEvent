using QuickEvent.Models;

namespace QuickEvent.Repositories.Interfaces
{
    public interface IOrganizerRequestRepository
    {
        Task AddRequestAsync(OrganizerRequest request);
        Task<List<OrganizerRequest>> GetPendingRequestsAsync();
        Task<OrganizerRequest> GetRequestByIdAsync(int id);
        Task UpdateRequestAsync(OrganizerRequest request);
        Task<OrganizerRequest> GetPendingRequestByUserIdAsync(string userId); // Thêm phương thức này
    }
}