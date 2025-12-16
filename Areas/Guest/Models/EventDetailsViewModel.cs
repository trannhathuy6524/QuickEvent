using QuickEvent.Models;

namespace QuickEvent.Areas.Guest.Models
{
    public class EventDetailsViewModel
    {
        public Event Event { get; set; }
        public Registration Registration { get; set; }
    }
}