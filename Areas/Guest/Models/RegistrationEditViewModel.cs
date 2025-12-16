using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Areas.Guest.Models
{
    public class RegistrationEditViewModel
    {
        public int Id { get; set; }

        public int EventId { get; set; }

        public string EventTitle { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public string AdditionalInfo { get; set; }
    }
}