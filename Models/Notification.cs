using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } // ID của người nhận thông báo (Guest hoặc Organizer)

        [Required]
        [StringLength(500)]
        public string Message { get; set; } // Nội dung thông báo

        [Required]
        [StringLength(50)]
        public string Type { get; set; }

        public int? RegistrationId { get; set; } // Liên kết với đăng ký (nếu có)

        public int? EventId { get; set; } // Liên kết với sự kiện (nếu có)

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false; // Trạng thái đã đọc

        public virtual ApplicationUser User { get; set; }
        public virtual Registration Registration { get; set; }
        public virtual Event Event { get; set; }
    }
}