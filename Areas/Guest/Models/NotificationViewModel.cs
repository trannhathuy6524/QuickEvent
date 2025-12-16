namespace QuickEvent.Areas.Guest.Models
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public string EventTitle { get; set; }
    }
}