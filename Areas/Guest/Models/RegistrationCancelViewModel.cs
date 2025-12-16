using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Areas.Guest.Models
{
    public class RegistrationCancelViewModel
    {
        public int RegistrationId { get; set; }

        public string EventTitle { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }
    }
}