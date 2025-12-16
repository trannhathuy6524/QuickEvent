using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Models
{
    public class Registration
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public string? AdditionalInfo { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public DateTime? LastModifiedDate { get; set; }

        public string? CancellationReason { get; set; }

        public DateTime? CancellationDate { get; set; }

        public string QRCodeToken { get; set; }

        public virtual Event Event { get; set; }

        public virtual ApplicationUser User { get; set; }
    }
}