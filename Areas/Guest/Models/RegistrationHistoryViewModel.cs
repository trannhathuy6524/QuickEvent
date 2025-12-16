namespace QuickEvent.Areas.Guest.Models
{
    public class RegistrationHistoryViewModel
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public DateTime EventStartDate { get; set; }
        public string EventLocation { get; set; }
        public DateTime RegistrationDate { get; set; }
        public bool IsCancelled { get; set; }
        public string CancellationReason { get; set; }
        public bool HasCheckedIn { get; set; }
        public DateTime? CheckInTime { get; set; }
    }
}