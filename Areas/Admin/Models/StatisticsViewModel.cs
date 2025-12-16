namespace QuickEvent.Areas.Admin.Models
{
    public class StatisticsViewModel
    {
        public int TotalEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int ActiveUsers { get; set; }
        public int PendingEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int OngoingEvents { get; set; }
        public int PendingOrganizerRequests { get; set; }
        public int NewUsersLast7Days { get; set; }
        public int NewRegistrationsLast24Hours { get; set; }
        public List<EventRegistrationStats> EventRegistrations { get; set; } = new List<EventRegistrationStats>();
    }
}