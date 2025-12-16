namespace QuickEvent.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public int MaxAttendees { get; set; }
        public bool IsPublic { get; set; }
        public string OrganizerId { get; set; }
        public string? CoverImagePath { get; set; }
        public bool IsRegistrationOpen { get; set; } = true;
        public string Status { get; set; } = "Đang mở"; // Pending, Approved, Rejected
        public bool IsCancelled { get; set; } = false;
        public virtual ApplicationUser Organizer { get; set; }
        public virtual IList<Registration> Registrations { get; set; }
    }
}