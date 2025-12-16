using QuickEvent.Models;

namespace QuickEvent.Repositories.Interfaces
{
    public interface IApplicationUserRepository
    {
        Task<List<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser> GetUserByIdAsync(string id);
        Task BlockUserAsync(string id);
        Task UnblockUserAsync(string id);
    }
}