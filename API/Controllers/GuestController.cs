using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;
using QuickEvent.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace QuickEvent.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GuestController : ControllerBase
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QRCodeService _qrCodeService;

        public GuestController(
            IEventRepository eventRepository,
            IRegistrationRepository registrationRepository,
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            QRCodeService qrCodeService)
        {
            _eventRepository = eventRepository;
            _registrationRepository = registrationRepository;
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _qrCodeService = qrCodeService;
        }

        // GET: api/guest/test - Test endpoint to verify API is working
        [HttpGet("test")]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Guest API is working correctly",
                timestamp = DateTime.Now,
                endpoints = new[]
                {
                    "GET /api/guest/events - Get all public events",
                    "GET /api/guest/events/{id} - Get event details",
                    "POST /api/guest/events/{id}/register - Register for event (Auth required)",
                    "GET /api/guest/registrations - Get my registrations (Auth required)",
                    "PUT /api/guest/registrations/{id}/cancel - Cancel registration (Auth required)"
                }
            });
        }

        // GET: api/guest/events
        [HttpGet("events")]
        [AllowAnonymous] // Cho phép truy cập không cần đăng nhập
        public async Task<ActionResult<IEnumerable<object>>> GetPublicEvents()
        {
            try
            {
                var events = await _eventRepository.SearchEventsAsync(null, true);
                var result = events.Select(e => new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    e.StartDate,
                    e.EndDate,
                    e.Location,
                    e.MaxAttendees,
                    e.IsRegistrationOpen,
                    e.Status,
                    CurrentRegistrations = e.Registrations?.Count(r => r.CancellationDate == null) ?? 0,
                    IsAvailable = e.IsRegistrationOpen && (e.Registrations?.Count(r => r.CancellationDate == null) ?? 0) < e.MaxAttendees
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sự kiện", error = ex.Message });
            }
        }

        // GET: api/guest/events/{id}
        [HttpGet("events/{id}")]
        [AllowAnonymous] // Cho phép xem chi tiết sự kiện không cần đăng nhập
        public async Task<ActionResult<object>> GetEventDetails(int id)
        {
            try
            {
                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || !eventItem.IsPublic)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện" });
                }

                var user = await _userManager.GetUserAsync(User);
                var isRegistered = false;
                if (user != null)
                {
                    var userRegistrations = await _registrationRepository.GetRegistrationsByUserAsync(user.Id);
                    isRegistered = userRegistrations.Any(r => r.EventId == id && r.CancellationDate == null);
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
                    eventItem.IsRegistrationOpen,
                    eventItem.Status,
                    CurrentRegistrations = eventItem.Registrations?.Count(r => r.CancellationDate == null) ?? 0,
                    IsAvailable = eventItem.IsRegistrationOpen && (eventItem.Registrations?.Count(r => r.CancellationDate == null) ?? 0) < eventItem.MaxAttendees,
                    IsRegistered = isRegistered,
                    Organizer = new
                    {
                        eventItem.Organizer?.FullName,
                        eventItem.Organizer?.Email
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin sự kiện", error = ex.Message });
            }
        }

        // POST: api/guest/events/{id}/register
        [HttpPost("events/{id}/register")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RegisterForEvent(int id, [FromBody] RegistrationRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || !eventItem.IsPublic)
                {
                    return NotFound(new { message = "Không tìm thấy sự kiện" });
                }

                if (!eventItem.IsRegistrationOpen)
                {
                    return BadRequest(new { message = "Đăng ký cho sự kiện này đã đóng" });
                }

                if (eventItem.Registrations?.Count >= eventItem.MaxAttendees)
                {
                    return BadRequest(new { message = "Sự kiện đã đủ số lượng người tham gia" });
                }

                // Kiểm tra đã đăng ký chưa
                var userRegistrations = await _registrationRepository.GetRegistrationsByUserAsync(user.Id);
                var existingRegistration = userRegistrations.FirstOrDefault(r => r.EventId == id && r.CancellationDate == null);
                if (existingRegistration != null)
                {
                    return BadRequest(new { message = "Bạn đã đăng ký sự kiện này rồi" });
                }

                var registration = new Registration
                {
                    EventId = id,
                    UserId = user.Id,
                    FullName = request.FullName ?? user.FullName,
                    Email = request.Email ?? user.Email,
                    PhoneNumber = request.PhoneNumber,
                    AdditionalInfo = request.AdditionalInfo,
                    QRCodeToken = $"temp_{Guid.NewGuid()}" // Tạo token tạm thời
                };

                await _registrationRepository.AddRegistrationAsync(registration);

                // Cập nhật QRCodeToken chính thức với signature
                registration.QRCodeToken = _qrCodeService.GenerateQRCodeToken(registration.Id, registration.EventId);
                await _registrationRepository.UpdateRegistrationAsync(registration);

                // ✅ THÊM: Sinh QR Code Image
                var qrCodeBytes = _qrCodeService.GenerateQRCodeImage(registration.QRCodeToken);
                var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

                // Gửi thông báo cho organizer
                var notification = new Notification
                {
                    UserId = eventItem.OrganizerId,
                    Message = $"Người dùng {registration.FullName} đã đăng ký tham gia sự kiện '{eventItem.Title}'",
                    Type = "EventRegistration",
                    EventId = id,
                    RegistrationId = registration.Id
                };
                await _notificationRepository.AddNotificationAsync(notification);

                // Trả về full registration object với QR Code Image
                return Ok(new
                {
                    registration.Id,
                    EventId = registration.EventId,
                    UserId = registration.UserId,
                    FullName = registration.FullName,
                    Email = registration.Email,
                    PhoneNumber = registration.PhoneNumber,
                    AdditionalInfo = registration.AdditionalInfo,
                    QRCodeToken = registration.QRCodeToken,
                    QRCodeImage = qrCodeBase64, // ✅ THÊM: Base64 QR Code Image
                    RegistrationDate = registration.RegistrationDate,
                    CancellationDate = registration.CancellationDate,
                    CancellationReason = registration.CancellationReason,
                    Event = new
                    {
                        eventItem.Id,
                        eventItem.Title,
                        Description = eventItem.Description,
                        StartDate = eventItem.StartDate,
                        EndDate = eventItem.EndDate,
                        Location = eventItem.Location,
                        MaxAttendees = eventItem.MaxAttendees,
                        IsPublic = eventItem.IsPublic,
                        IsRegistrationOpen = eventItem.IsRegistrationOpen,
                        Status = eventItem.Status,
                        IsCancelled = eventItem.IsCancelled,
                        CurrentRegistrations = eventItem.Registrations?.Count(r => r.CancellationDate == null) ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đăng ký sự kiện", error = ex.Message });
            }
        }

        // GET: api/guest/registrations
        [HttpGet("registrations")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<IEnumerable<object>>> GetMyRegistrations()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // Trả về empty array nếu chưa đăng nhập
                    return Ok(new List<object>());
                }

                var registrations = await _registrationRepository.GetRegistrationsByUserAsync(user.Id);
                var result = registrations.Select(r => new
                {
                    r.Id,
                    EventId = r.EventId, // Thêm EventId
                    UserId = r.UserId, // Thêm UserId
                    r.FullName,
                    r.Email,
                    r.PhoneNumber,
                    r.AdditionalInfo,
                    r.RegistrationDate,
                    r.QRCodeToken,
                    r.CancellationDate,
                    r.CancellationReason,
                    Event = new
                    {
                        r.Event.Id,
                        r.Event.Title,
                        Description = r.Event.Description,
                        r.Event.StartDate,
                        r.Event.EndDate,
                        r.Event.Location,
                        MaxAttendees = r.Event.MaxAttendees,
                        IsPublic = r.Event.IsPublic,
                        IsRegistrationOpen = r.Event.IsRegistrationOpen,
                        r.Event.Status,
                        IsCancelled = r.Event.IsCancelled,
                        CurrentRegistrations = r.Event.Registrations?.Count(reg => reg.CancellationDate == null) ?? 0
                    }
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách đăng ký", error = ex.Message });
            }
        }

        // PUT: api/guest/registrations/{id}/cancel
        [HttpPut("registrations/{id}/cancel")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> CancelRegistration(int id, [FromBody] CancellationRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var registration = await _registrationRepository.GetRegistrationByIdAsync(id);
                if (registration == null || registration.UserId != user.Id)
                {
                    return NotFound(new { message = "Không tìm thấy đăng ký" });
                }

                if (registration.CancellationDate != null)
                {
                    return BadRequest(new { message = "Đăng ký đã được hủy trước đó" });
                }

                await _registrationRepository.CancelRegistrationAsync(id, request.Reason);

                // Gửi thông báo cho organizer
                var notification = new Notification
                {
                    UserId = registration.Event.OrganizerId,
                    Message = $"Người dùng {registration.FullName} đã hủy đăng ký sự kiện '{registration.Event.Title}'",
                    Type = "RegistrationCancelled",
                    EventId = registration.EventId,
                    RegistrationId = registration.Id
                };
                await _notificationRepository.AddNotificationAsync(notification);

                return Ok(new { message = "Hủy đăng ký thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi hủy đăng ký", error = ex.Message });
            }
        }

        // GET: api/guest/notifications
        [HttpGet("notifications")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<IEnumerable<object>>> GetNotifications()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // Trả về empty array nếu chưa đăng nhập
                    return Ok(new List<object>());
                }

                var notifications = await _notificationRepository.GetNotificationsByUserAsync(user.Id);
                var result = notifications.Select(n => new
                {
                    n.Id,
                    n.Message,
                    n.Type,
                    n.CreatedDate,
                    n.IsRead,
                    Event = n.Event != null ? new { n.Event.Id, n.Event.Title } : null
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông báo", error = ex.Message });
            }
        }

        // PUT: api/guest/notifications/{id}/mark-read
        [HttpPut("notifications/{id}/mark-read")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

        // GET: api/guest/registrations/{id}/qrcode - Lấy QR Code của đăng ký
        [HttpGet("registrations/{id}/qrcode")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetRegistrationQRCode(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized(new { message = "Người dùng không được xác thực" });
                }

                var registration = await _registrationRepository.GetRegistrationByIdAsync(id);
                if (registration == null || registration.UserId != user.Id)
                {
                    return NotFound(new { message = "Không tìm thấy đăng ký" });
                }

                if (registration.CancellationDate != null)
                {
                    return BadRequest(new { message = "Đăng ký đã bị hủy, không thể lấy QR Code" });
                }

                // Sinh QR Code Image
                var qrCodeBytes = _qrCodeService.GenerateQRCodeImage(registration.QRCodeToken);
                var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

                return Ok(new
                {
                    RegistrationId = registration.Id,
                    EventId = registration.EventId,
                    EventTitle = registration.Event?.Title,
                    QRCodeToken = registration.QRCodeToken,
                    QRCodeImage = qrCodeBase64
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy QR Code", error = ex.Message });
            }
        }
    }

    public class RegistrationRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AdditionalInfo { get; set; }
    }

    public class CancellationRequest
    {
        public string Reason { get; set; }
    }
}