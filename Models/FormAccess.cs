using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Models
{
    public class FormAccess
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        public DateTime AccessTime { get; set; } = DateTime.Now;

        public virtual Event Event { get; set; }
    }
}