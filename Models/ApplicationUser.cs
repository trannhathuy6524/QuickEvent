using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đầy đủ của bạn.")]
        [StringLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự.")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string? PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        //[Required(ErrorMessage = "Vui lòng chỉ định loại người dùng.")]
        public string UserType { get; set; } = "Guest";

        public bool IsApproved { get; set; } = false;
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public virtual ICollection<OrganizerRequest> OrganizerRequests { get; set; } = new List<OrganizerRequest>();
    }
}