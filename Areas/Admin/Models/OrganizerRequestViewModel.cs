namespace QuickEvent.Areas.Admin.Models
{
    public class OrganizerRequestViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
    }
}