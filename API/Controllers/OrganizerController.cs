using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ICheckInRepository _checkInRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrganizerController(
            IEventRepository eventRepository,
            IRegistrationRepository registrationRepository,
            INotificationRepository notificationRepository,
            ICheckInRepository checkInRepository,
            UserManager<ApplicationUser> userManager)
        {
            _eventRepository = eventRepository;
            _registrationRepository = registrationRepository;
            _notificationRepository = notificationRepository;
            _checkInRepository = checkInRepository;
            _userManager = userManager;
        }

        // GET: api/organizer/events
        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyEvents()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var events = await _eventRepository.GetEventsByOrganizerAsync(user.Id);
                var result = new List<object>();

                foreach (var e in events)
                {
                    var checkIns = await _checkInRepository.GetCheckInsByEventAsync(e.Id);
                    result.Add(new
                    {
                        e.Id,
                        e.Title,
                        e.Description,
                        e.StartDate,
                        e.EndDate,
                        e.Location,
                        e.MaxAttendees,
                        e.IsPublic,
                        e.IsRegistrationOpen,
                        e.Status,
                        e.IsCancelled,
                        CurrentRegistrations = e.Registrations?.Count ?? 0,
                        CheckedInCount = checkIns.Count
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sự kiện", error = ex.Message });
            }
        }

        // POST: api/organizer/events
        [HttpPost("events")]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var eventItem = new Event
                {
                    Title = request.Title,
                    Description = request.Description,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Location = request.Location,
                    MaxAttendees = request.MaxAttendees,
                    IsPublic = request.IsPublic,
                    OrganizerId = user.Id,
                    IsRegistrationOpen = true,
                    Status = "Mở"
                };

                await _eventRepository.AddEventAsync(eventItem);

                return Ok(new { message = "Tạo sự kiện thành công", eventId = eventItem.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo sự kiện", error = ex.Message });
            }
        }

        // GET: api/organizer/events/{id}
        [HttpGet("events/{id}")]
        public async Task<ActionResult<object>> GetEventDetails(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != user.Id)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện" });
                }

                var checkIns = await _checkInRepository.GetCheckInsByEventAsync(id);
                var registrations = new List<object>();

                if (eventItem.Registrations != null)
                {
                    foreach (var r in eventItem.Registrations)
                    {
                        var userCheckIn = await _checkInRepository.GetCheckInByRegistrationId(r.Id);
                        registrations.Add(new
                        {
                            r.Id,
                            r.FullName,
                            r.Email,
                            r.PhoneNumber,
                            r.RegistrationDate,
                            IsCheckedIn = userCheckIn != null
                        });
                    }
                }

                var result = new
                {
                    eventItem.Id,
                    eventItem.Title,
                    eventItem.Description,
                    eventItem.StartDate,
                    eventItem.EndDate,
                    eventItem.Location,
                    eventItem.MaxAttendees,
                    eventItem.IsPublic,
                    eventItem.IsRegistrationOpen,
                    eventItem.Status,
                    eventItem.IsCancelled,
                    CurrentRegistrations = eventItem.Registrations?.Count ?? 0,
                    CheckedInCount = checkIns.Count,
                    Registrations = registrations
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin sự kiện", error = ex.Message });
            }
        }

        // PUT: api/organizer/events/{id}
        [HttpPut("events/{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != user.Id)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện" });
                }

                eventItem.Title = request.Title;
                eventItem.Description = request.Description;
                eventItem.StartDate = request.StartDate;
                eventItem.EndDate = request.EndDate;
                eventItem.Location = request.Location;
                eventItem.MaxAttendees = request.MaxAttendees;
                eventItem.IsPublic = request.IsPublic;
                eventItem.IsRegistrationOpen = request.IsRegistrationOpen;

                await _eventRepository.UpdateEventAsync(eventItem);

                return Ok(new { message = "Cập nhật sự kiện thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật sự kiện", error = ex.Message });
            }
        }

        // DELETE: api/organizer/events/{id}
        [HttpDelete("events/{id}")]
        public async Task<IActionResult> CancelEvent(int id, [FromBody] CancelEventRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != user.Id)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện" });
                }

                eventItem.IsCancelled = true;
                eventItem.Status = "Đã hủy";
                eventItem.IsRegistrationOpen = false;
                await _eventRepository.UpdateEventAsync(eventItem);

                // Gửi thông báo cho tất cả người đăng ký
                var registrations = await _registrationRepository.GetRegistrationsByEventAsync(id);
                foreach (var registration in registrations)
                {
                    var notification = new Notification
                    {
                        UserId = registration.UserId,
                        Message = $"Sự kiện '{eventItem.Title}' đã bị hủy. Lý do: {request.Reason}",
                        Type = "EventCancelled",
                        EventId = id
                    };
                    await _notificationRepository.AddNotificationAsync(notification);
                }

                return Ok(new { message = "Hủy sự kiện thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi hủy sự kiện", error = ex.Message });
            }
        }

        // GET: api/organizer/events/{id}/registrations
        [HttpGet("events/{id}/registrations")]
        public async Task<ActionResult<IEnumerable<object>>> GetEventRegistrations(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != user.Id)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện" });
                }

                var registrations = await _registrationRepository.GetRegistrationsByEventAsync(id);
                var result = new List<object>();

                foreach (var r in registrations)
                {
                    var checkIn = await _checkInRepository.GetCheckInByRegistrationId(r.Id);
                    result.Add(new
                    {
                        r.Id,
                        r.FullName,
                        r.Email,
                        r.PhoneNumber,
                        r.AdditionalInfo,
                        r.RegistrationDate,
                        r.CancellationDate,
                        r.CancellationReason,
                        r.QRCodeToken,
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách đăng ký", error = ex.Message });
            }
        }

        // POST: api/organizer/checkin
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckInParticipant([FromBody] CheckInRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                // Tìm registration bằng QR code token
                var registrations = await _registrationRepository.GetRegistrationsByEventAsync(0); // Lấy tất cả để tìm
                var registration = registrations.FirstOrDefault(r => r.QRCodeToken == request.QRCodeToken);

                if (registration == null)
                {
                    return NotFound(new { message = "Không tìm thấy đăng ký với mã QR này" });
                }

                if (registration.Event.OrganizerId != user.Id)
                {
                    return Forbid("Bạn không có quyền check-in cho sự kiện này");
                }

                // Kiểm tra đã check-in chưa
                var existingCheckIn = await _checkInRepository.GetCheckInByRegistrationId(registration.Id);
                if (existingCheckIn != null)
                {
                    return BadRequest(new { message = "Người tham gia đã check-in rồi" });
                }

                var checkIn = new CheckIn
                {
                    RegistrationId = registration.Id,
                    EventId = registration.EventId,
                    CheckInTime = DateTime.Now
                };

                await _checkInRepository.AddCheckInAsync(checkIn);

                return Ok(new 
                { 
                    message = "Check-in thành công",
                    participant = new
                    {
                        registration.FullName,
                        registration.Email,
                        EventTitle = registration.Event.Title,
                        CheckInTime = checkIn.CheckInTime
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi check-in", error = ex.Message });
            }
        }

        // GET: api/organizer/notifications
        [HttpGet("notifications")]
        public async Task<ActionResult<IEnumerable<object>>> GetNotifications()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var notifications = await _notificationRepository.GetNotificationsByUserAsync(user.Id);
                var result = notifications.Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.Type,
                    n.CreatedDate,
                    n.IsRead,
                    Event = n.Event != null ? new { n.Event.Id, n.Event.Title } : null,
                    Registration = n.Registration != null ? new { n.Registration.Id, n.Registration.FullName } : null
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông báo", error = ex.Message });
            }
        }

        // PUT: api/organizer/notifications/{id}/mark-read
        [HttpPut("notifications/{id}/mark-read")]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                await _notificationRepository.MarkAsReadAsync(id);

                return Ok(new { message = "Đánh dấu đã đọc thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đánh dấu thông báo", error = ex.Message });
            }
        }

        // GET: api/organizer/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var events = await _eventRepository.GetEventsByOrganizerAsync(user.Id);
                var totalRegistrations = 0;
                var totalCheckIns = 0;

                foreach (var eventItem in events)
                {
                    var registrations = await _registrationRepository.GetRegistrationsByEventAsync(eventItem.Id);
                    totalRegistrations += registrations.Count();

                    var checkIns = await _checkInRepository.GetCheckInsByEventAsync(eventItem.Id);
                    totalCheckIns += checkIns.Count();
                }

                var result = new
                {
                    TotalEvents = events.Count(),
                    ActiveEvents = events.Count(e => !e.IsCancelled && e.IsRegistrationOpen),
                    TotalRegistrations = totalRegistrations,
                    TotalCheckIns = totalCheckIns,
                    AttendanceRate = totalRegistrations > 0 ? (double)totalCheckIns / totalRegistrations * 100 : 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thống kê", error = ex.Message });
            }
        }
    }

    public class CreateEventRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Location { get; set; }
        public int MaxAttendees { get; set; }
        public bool IsPublic { get; set; } = true;
    }

    public class UpdateEventRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Location { get; set; }
        public int MaxAttendees { get; set; }
        public bool IsPublic { get; set; }
        public bool IsRegistrationOpen { get; set; }
    }

    public class CancelEventRequest
    {
        public string Reason { get; set; }
    }

    public class CheckInRequest
    {
        public string QRCodeToken { get; set; }
        public string? Location { get; set; }
    }
}