using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Areas.Guest.Models
{
    public class RegistrationViewModel
    {
        public int EventId { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Phone]
        public string? PhoneNumber { get; set; }
        public string? AdditionalInfo { get; set; }
    }
}