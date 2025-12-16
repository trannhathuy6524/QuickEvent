using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Areas.Admin.Models
{
    public class UserManagementViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phân quyền")]
        [Display(Name = "Phân quyền")]
        public string SelectedRole { get; set; }

        // Bỏ hoàn toàn mọi validation cho UserType
        [Display(Name = "Loại người dùng")]
        public string UserType { get; set; } = "Guest"; // Gán giá trị mặc định

        public DateTime RegistrationDate { get; set; }
        public bool IsLocked { get; set; }
        public bool IsApproved { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> AvailableRoles => new() { "Guest", "Organizer" };
    }
}