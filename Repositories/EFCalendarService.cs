using QuickEvent.Models;

namespace QuickEvent.Repositories
{
    public class EFCalendarService : ICalendarService
    {
        public string GenerateIcsContent(Event @event)
        {
            var startDate = @event.StartDate.ToUniversalTime();
            var endDate = @event.EndDate?.ToUniversalTime() ?? startDate.AddHours(1);
            var icsContent = "BEGIN:VCALENDAR\r\n" +
                             "VERSION:2.0\r\n" +
                             "METHOD:PUBLISH\r\n" + // Thêm METHOD:PUBLISH
                             "PRODID:-//QuickEvent//Event Calendar//EN\r\n" +
                             "BEGIN:VEVENT\r\n" +
                             $"UID:{Guid.NewGuid()}@quickevent.com\r\n" +
                             $"DTSTAMP:{DateTime.Now:yyyyMMddTHHmmssZ}\r\n" +
                             $"DTSTART:{startDate:yyyyMMddTHHmmssZ}\r\n" +
                             $"DTEND:{endDate:yyyyMMddTHHmmssZ}\r\n" +
                             $"SUMMARY:{@event.Title?.Replace(",", "\\,").Replace(";", "\\;")}\r\n" + // Escape thêm dấu chấm phẩy
                             $"DESCRIPTION:{@event.Description?.Replace("\n", "\\n").Replace(",", "\\,").Replace(";", "\\;") ?? "Không có mô tả"}\r\n" +
                             $"LOCATION:{@event.Location?.Replace(",", "\\,").Replace(";", "\\;").Replace("/", "\\/") ?? "Không có địa điểm"}\r\n" + // Escape dấu /
                             "END:VEVENT\r\n" +
                             "END:VCALENDAR";
            return icsContent;
        }
    }
}