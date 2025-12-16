using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Models
{
    public class CheckIn
    {
        public int Id { get; set; }

        [Required]
        public int RegistrationId { get; set; }

        [Required]
        public int EventId { get; set; }

        public DateTime CheckInTime { get; set; } = DateTime.Now;

        public virtual Registration Registration { get; set; }
        public virtual Event Event { get; set; }
    }
}