using QuickEvent.Models;

namespace QuickEvent.Repositories.Interfaces
{
    public interface IFormAccessRepository
    {
        Task AddFormAccessAsync(FormAccess formAccess);
        Task<int> GetFormAccessCountAsync(int eventId);
    }
}