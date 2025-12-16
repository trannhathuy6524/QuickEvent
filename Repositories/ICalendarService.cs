using QuickEvent.Models;

namespace QuickEvent.Repositories
{
    public interface ICalendarService
    {
        string GenerateIcsContent(Event @event);
    }
}