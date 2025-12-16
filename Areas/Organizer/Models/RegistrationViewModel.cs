namespace QuickEvent.Areas.Organizer.Models
{
    public class RegistrationViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AdditionalInfo { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string EventTitle { get; set; }
        public DateTime? CancellationDate { get; set; }
    }
}