namespace QuickEvent.Areas.Organizer.Models
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public int? EventId { get; set; }  // Nullable vì có thể không liên quan đến event
        public string? EventTitle { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public string? NotificationType { get; set; }  // Thêm trường để phân loại thông báo
    }
}