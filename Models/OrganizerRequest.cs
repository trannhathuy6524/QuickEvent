using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Models
{
    public class OrganizerRequest
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; }

        public string Status { get; set; } = "Chờ duyệt"; // Pending, Approved, Rejected

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public virtual ApplicationUser User { get; set; }
    }
}