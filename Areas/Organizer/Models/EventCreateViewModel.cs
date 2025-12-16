using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Areas.Organizer.Models
{
    public class EventCreateViewModel
    {
        [Required]
        public string Title { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Description { get; set; }
        [Required]
        public string Location { get; set; }
        [Range(1, int.MaxValue)]
        public int MaxAttendees { get; set; }
        public bool IsPublic { get; set; }
    }
}