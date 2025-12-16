using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using QuickEvent.Areas.Organizer.Models;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;
using System.Text;

namespace QuickEvent.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class RegistrationController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICheckInRepository _checkInRepository;
        private readonly IFormAccessRepository _formAccessRepository;

        public RegistrationController(IEventRepository eventRepository,
            IRegistrationRepository registrationRepository,
            UserManager<ApplicationUser> userManager,
            ICheckInRepository checkInRepository,
            IFormAccessRepository formAccessRepository)
        {
            _eventRepository = eventRepository;
            _registrationRepository = registrationRepository;
            _userManager = userManager;
            _checkInRepository = checkInRepository;
            _formAccessRepository = formAccessRepository;
        }

        // Xem danh sách người đăng ký
        public async Task<IActionResult> Index(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("Không tìm thấy người dùng hiện tại.");
            }

            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
            {
                return NotFound("Không tìm thấy sự kiện hoặc bạn không có quyền xem.");
            }

            var registrations = await _registrationRepository.GetRegistrationsByEventAsync(eventId);
            var model = registrations.Select(r => new RegistrationViewModel
            {
                Id = r.Id,
                FullName = r.FullName,
                Email = r.Email,
                PhoneNumber = r.PhoneNumber,
                AdditionalInfo = r.AdditionalInfo,
                RegistrationDate = r.RegistrationDate,
                CancellationDate = r.CancellationDate,
                EventTitle = r.Event.Title
            }).ToList();

            ViewBag.EventTitle = @event.Title;
            ViewBag.EventId = eventId;
            return View(model);
        }

        // Xem chi tiết người đăng ký
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("Không tìm thấy người dùng hiện tại.");
            }

            var registration = await _registrationRepository.GetRegistrationByIdAsync(id);
            if (registration == null || registration.Event.OrganizerId != currentUser.Id)
            {
                return NotFound("Không tìm thấy đăng ký hoặc bạn không có quyền xem.");
            }

            var model = new RegistrationViewModel
            {
                Id = registration.Id,
                FullName = registration.FullName,
                Email = registration.Email,
                PhoneNumber = registration.PhoneNumber,
                AdditionalInfo = registration.AdditionalInfo,
                RegistrationDate = registration.RegistrationDate,
                CancellationDate = registration.CancellationDate,
                EventTitle = registration.Event.Title
            };

            ViewBag.EventId = registration.EventId;
            return View(model);
        }

        // Xuất danh sách dạng CSV
        public async Task<IActionResult> ExportCsv(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("Không tìm thấy người dùng hiện tại.");
            }

            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
            {
                return NotFound("Không tìm thấy sự kiện hoặc bạn không có quyền xuất.");
            }

            var registrations = await _registrationRepository.GetRegistrationsByEventAsync(eventId);
            var builder = new StringBuilder();
            builder.AppendLine("ID,FullName,Email,PhoneNumber,AdditionalInfo,RegistrationDate,Status");

            foreach (var reg in registrations)
            {
                var status = reg.CancellationDate == null ? "Tham gia" : "Hủy";
                builder.AppendLine($"{reg.Id},\"{reg.FullName}\",\"{reg.Email}\",\"{reg.PhoneNumber}\",\"{reg.AdditionalInfo?.Replace("\"", "\"\"")}\",\"{reg.RegistrationDate:yyyy-MM-dd HH:mm}\",\"{status}\"");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Registrations_{@event.Title}.csv");
        }

        // Xuất danh sách dạng Excel
        public async Task<IActionResult> ExportExcel(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound("Không tìm thấy người dùng hiện tại.");
            }

            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
            {
                return NotFound("Không tìm thấy sự kiện hoặc bạn không có quyền xuất.");
            }

            var registrations = await _registrationRepository.GetRegistrationsByEventAsync(eventId);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Registrations");
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Full Name";
                worksheet.Cells[1, 3].Value = "Email";
                worksheet.Cells[1, 4].Value = "Phone Number";
                worksheet.Cells[1, 5].Value = "Additional Info";
                worksheet.Cells[1, 6].Value = "Registration Date";
                worksheet.Cells[1, 7].Value = "Status";

                for (int i = 0; i < registrations.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = registrations[i].Id;
                    worksheet.Cells[i + 2, 2].Value = registrations[i].FullName;
                    worksheet.Cells[i + 2, 3].Value = registrations[i].Email;
                    worksheet.Cells[i + 2, 4].Value = registrations[i].PhoneNumber;
                    worksheet.Cells[i + 2, 5].Value = registrations[i].AdditionalInfo;
                    worksheet.Cells[i + 2, 6].Value = registrations[i].RegistrationDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cells[i + 2, 7].Value = registrations[i].CancellationDate == null ? "Tham gia" : "Hủy";
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Registrations_{@event.Title}.xlsx");
            }
        }

        public async Task<IActionResult> Report(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
                return NotFound();

            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var now = TimeZoneInfo.ConvertTime(DateTime.Now, timeZoneInfo);
            var today = now.Date;

            // Tuần hiện tại (thứ Hai đến Chủ Nhật)
            var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);

            // Tháng hiện tại (4 tuần từ tuần chứa ngày 1)
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var firstWeekStart = monthStart.AddDays(-(int)monthStart.DayOfWeek + (int)DayOfWeek.Monday);
            var monthEnd = firstWeekStart.AddDays(28).AddTicks(-1);

            var registrationsByDay = await _eventRepository.GetRegistrationsByDayAsync(eventId, weekStart, weekEnd);
            var registrationsByWeek = await _eventRepository.GetRegistrationsByWeekAsync(eventId, firstWeekStart, monthEnd);
            var registrationCount = await _eventRepository.GetRegistrationCountAsync(eventId);
            var formAccessCount = await _formAccessRepository.GetFormAccessCountAsync(eventId);
            var checkIns = await _checkInRepository.GetCheckInsByEventAsync(eventId);

            Console.WriteLine($"Report: EventId={eventId}, RegistrationCount={registrationCount}, CheckInCount={checkIns.Count}");
            Console.WriteLine($"Weekly Registrations: {string.Join(", ", registrationsByWeek.Select(kv => $"{kv.Key:yyyy-MM-dd}:{kv.Value}"))}");

            var formCompletionRate = formAccessCount > 0 ? (double)registrationCount / formAccessCount * 100 : 0;
            var noShowRate = registrationCount > 0
                ? Math.Max(0, (double)(registrationCount - checkIns.Count) / registrationCount * 100)
                : 0;

            Console.WriteLine($"Report: EventId={eventId}, NoShowRate={noShowRate:F2}%");

            var model = new ReportViewModel
            {
                EventId = eventId,
                EventTitle = @event.Title,
                RegistrationsByDay = registrationsByDay ?? new Dictionary<DateTime, int>(),
                RegistrationsByWeek = registrationsByWeek ?? new Dictionary<DateTime, int>(),
                FormCompletionRate = formCompletionRate,
                NoShowRate = noShowRate
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetRegistrationsByDay(int eventId, DateTime startDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
                return Unauthorized();

            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            startDate = TimeZoneInfo.ConvertTime(startDate, timeZoneInfo);
            var weekStart = startDate.Date;
            var weekEnd = weekStart.AddDays(7).AddTicks(-1);

            var registrations = await _eventRepository.GetRegistrationsByDayAsync(eventId, weekStart, weekEnd);
            var result = registrations.ToDictionary(
                kvp => kvp.Key.ToString("yyyy-MM-dd"),
                kvp => kvp.Value
            );

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetRegistrationsByWeek(int eventId, DateTime startDate)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
                return Unauthorized();

            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            startDate = TimeZoneInfo.ConvertTime(startDate, timeZoneInfo);
            var firstWeekStart = startDate.Date;
            var monthEnd = firstWeekStart.AddDays(28).AddTicks(-1);

            var registrations = await _eventRepository.GetRegistrationsByWeekAsync(eventId, firstWeekStart, monthEnd);
            var result = registrations.ToDictionary(
                kvp => kvp.Key.ToString("yyyy-MM-dd"),
                kvp => kvp.Value
            );

            return Json(result);
        }
    }
}