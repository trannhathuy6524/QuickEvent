using System.ComponentModel.DataAnnotations;

namespace QuickEvent.Areas.Organizer.Models
{
    public class EventEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sự kiện là bắt buộc")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Địa điểm là bắt buộc")]
        public string Location { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số người tham gia tối đa phải lớn hơn 0")]
        public int MaxAttendees { get; set; }

        public bool IsPublic { get; set; }

        public bool IsRegistrationOpen { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string Status { get; set; }
    }
}