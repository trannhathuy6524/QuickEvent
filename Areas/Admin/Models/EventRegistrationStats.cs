namespace QuickEvent.Areas.Admin.Models
{
    public class EventRegistrationStats
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public int RegistrationCount { get; set; }
        public int MaxAttendees { get; set; }
        public string Status { get; set; }
    }
}
    