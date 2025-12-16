namespace QuickEvent.Areas.Organizer.Models
{
    public class ReportViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; }
        public Dictionary<DateTime, int> RegistrationsByDay { get; set; }
        public Dictionary<DateTime, int> RegistrationsByWeek { get; set; }
        public double FormCompletionRate { get; set; }
        public double NoShowRate { get; set; }
    }
}