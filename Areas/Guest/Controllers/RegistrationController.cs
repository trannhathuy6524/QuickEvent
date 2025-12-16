using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickEvent.Areas.Guest.Models;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;
using QuickEvent.Services;

namespace QuickEvent.Areas.Guest.Controllers
{
    [Area("Guest")]
    [Authorize(Roles = "Guest,Organizer")]
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEventRepository _eventRepository;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IFormAccessRepository _formAccessRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ICheckInRepository _checkInRepository;
        private readonly QRCodeService _qrCodeService;

        public RegistrationController(
            IEventRepository eventRepository,
            IRegistrationRepository registrationRepository,
            IFormAccessRepository formAccessRepository,
            INotificationRepository notificationRepository,
            ICheckInRepository checkInRepository,
            ApplicationDbContext context,
            QRCodeService qrCodeService)
        {
            _eventRepository = eventRepository;
            _registrationRepository = registrationRepository;
            _formAccessRepository = formAccessRepository;
            _notificationRepository = notificationRepository;
            _checkInRepository = checkInRepository;
            _context = context;
            _qrCodeService = qrCodeService;
        }

        public async Task<IActionResult> Index(int eventId)
        {
            var eventItem = await _eventRepository.GetEventByIdAsync(eventId);
            if (eventItem == null || !eventItem.IsPublic || !eventItem.IsRegistrationOpen || eventItem.IsCancelled)
            {
                TempData["Error"] = "Sự kiện không khả dụng hoặc đã đóng đăng ký!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var registrationCount = await _eventRepository.GetRegistrationCountAsync(eventId);
            if (registrationCount >= eventItem.MaxAttendees)
            {
                TempData["Error"] = "Sự kiện đã đạt giới hạn người đăng ký!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            await _formAccessRepository.AddFormAccessAsync(new FormAccess { EventId = eventId });

            var model = new RegistrationViewModel { EventId = eventId };
            return View(model);
        }

        // RegistrationController.cs
        [HttpPost]
        public async Task<IActionResult> Submit(RegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để đăng ký sự kiện.";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var eventItem = await _eventRepository.GetEventByIdAsync(model.EventId);
            if (eventItem == null || !eventItem.IsRegistrationOpen || eventItem.IsCancelled)
            {
                TempData["Error"] = "Sự kiện không khả dụng hoặc đã đóng đăng ký!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var registrationCount = await _eventRepository.GetRegistrationCountAsync(model.EventId);
            if (registrationCount >= eventItem.MaxAttendees)
            {
                TempData["Error"] = "Sự kiện đã đạt giới hạn người đăng ký!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var registration = new Registration
            {
                EventId = model.EventId,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                AdditionalInfo = model.AdditionalInfo,
                UserId = userId,
                RegistrationDate = DateTime.Now,
                QRCodeToken = $"temp_{Guid.NewGuid()}" // Tạo token tạm thời
            };

            try
            {
                await _registrationRepository.AddRegistrationAsync(registration);

                // Cập nhật QRCodeToken chính thức
                registration.QRCodeToken = _qrCodeService.GenerateQRCodeToken(registration.Id, registration.EventId);
                await _registrationRepository.UpdateRegistrationAsync(registration);

                await _eventRepository.UpdateEventStatusAsync(model.EventId);
                TempData["Message"] = "Đăng ký sự kiện thành công!";
                return RedirectToAction("Success", new { registrationId = registration.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi đăng ký: {ex.Message}";
                return View("Index", model);
            }
        }

        public async Task<IActionResult> Success(int registrationId)
        {
            var registration = await _registrationRepository.GetRegistrationByIdAsync(registrationId);
            if (registration == null)
                return NotFound();

            var qrCodeBytes = _qrCodeService.GenerateQRCodeImage(registration.QRCodeToken);
            ViewBag.QRCode = Convert.ToBase64String(qrCodeBytes);
            return View(registration);
        }

        [HttpGet]
        public async Task<IActionResult> GetQRCode(int registrationId)
        {
            var registration = await _registrationRepository.GetRegistrationByIdAsync(registrationId);
            if (registration == null || string.IsNullOrEmpty(registration.QRCodeToken))
                return NotFound();

            var qrCodeBytes = _qrCodeService.GenerateQRCodeImage(registration.QRCodeToken);
            return File(qrCodeBytes, "image/png");
        }

        public async Task<IActionResult> MyRegistrations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Challenge();
            }

            var registrations = await _registrationRepository.GetRegistrationsByUserAsync(userId);
            return View(registrations);
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(int registrationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Challenge();
            }

            var registration = await _registrationRepository.GetRegistrationByIdAsync(registrationId);
            if (registration == null || registration.UserId != userId)
                return NotFound();

            var eventItem = await _eventRepository.GetEventByIdAsync(registration.EventId);
            if (eventItem == null)
                return NotFound();

            if (eventItem.StartDate <= DateTime.Now)
            {
                TempData["Error"] = "Không thể hủy đăng ký vì sự kiện đã bắt đầu!";
                return RedirectToAction("MyRegistrations");
            }

            var model = new RegistrationCancelViewModel
            {
                RegistrationId = registrationId,
                EventTitle = eventItem.Title
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(RegistrationCancelViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Challenge();
            }

            var registration = await _registrationRepository.GetRegistrationByIdAsync(model.RegistrationId);
            if (registration == null || registration.UserId != userId)
                return NotFound();

            var eventItem = await _eventRepository.GetEventByIdAsync(registration.EventId);
            if (eventItem == null)
                return NotFound();

            if (eventItem.StartDate <= DateTime.Now)
            {
                TempData["Error"] = "Không thể hủy đăng ký vì sự kiện đã bắt đầu!";
                return RedirectToAction("MyRegistrations");
            }

            await _registrationRepository.CancelRegistrationAsync(model.RegistrationId, model.CancellationReason);

            var notification = new Notification
            {
                UserId = eventItem.OrganizerId,
                Message = $"Người dùng {registration.FullName} đã hủy đăng ký sự kiện '{eventItem.Title}'. Lý do: {model.CancellationReason ?? "Không cung cấp lý do"}. Email: {registration.Email}, SĐT: {registration.PhoneNumber ?? "N/A"}.",
                RegistrationId = model.RegistrationId,
                CreatedDate = DateTime.Now,
                Type = "RegistrationCancelled"
            };
            await _notificationRepository.AddNotificationAsync(notification);

            TempData["Message"] = "Đã hủy đăng ký sự kiện thành công!";
            return RedirectToAction("MyRegistrations");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int registrationId)
        {
            var registration = await _registrationRepository.GetRegistrationByIdAsync(registrationId);
            if (registration == null || registration.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                return NotFound();

            var eventItem = await _eventRepository.GetEventByIdAsync(registration.EventId);
            if (eventItem == null)
                return NotFound();

            var now = DateTime.Now;
            var canEdit = eventItem.StartDate > now && (registration.RegistrationDate.AddHours(24) > now);
            if (!canEdit)
            {
                TempData["Error"] = "Không thể chỉnh sửa thông tin đăng ký do đã hết thời gian cho phép!";
                return RedirectToAction("MyRegistrations");
            }

            var model = new RegistrationEditViewModel
            {
                Id = registration.Id,
                EventId = registration.EventId,
                EventTitle = eventItem.Title,
                FullName = registration.FullName,
                Email = registration.Email,
                PhoneNumber = registration.PhoneNumber,
                AdditionalInfo = registration.AdditionalInfo
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegistrationEditViewModel model)
        {
            try
            {
                ModelState.Remove("EventTitle");
                ModelState.Remove("Registration.User");
                ModelState.Remove("Registration.Event");

                if (!ModelState.IsValid)
                {
                    var eventDetails = await _eventRepository.GetEventByIdAsync(model.EventId);
                    model.EventTitle = eventDetails?.Title ?? "N/A";
                    return View(model);
                }

                var registration = await _registrationRepository.GetRegistrationByIdAsync(model.Id);
                if (registration == null || registration.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                {
                    return NotFound();
                }

                var eventInfo = await _eventRepository.GetEventByIdAsync(registration.EventId);
                if (eventInfo == null)
                {
                    return NotFound();
                }

                var now = DateTime.Now;
                var canEdit = eventInfo.StartDate > now && (registration.RegistrationDate.AddHours(24) > now);
                if (!canEdit)
                {
                    TempData["Error"] = "Không thể chỉnh sửa thông tin đăng ký do đã hết thời gian cho phép!";
                    return RedirectToAction(nameof(MyRegistrations));
                }

                registration.FullName = model.FullName;
                registration.Email = model.Email;
                registration.PhoneNumber = model.PhoneNumber;
                registration.AdditionalInfo = model.AdditionalInfo;
                registration.LastModifiedDate = DateTime.Now;

                await _registrationRepository.UpdateRegistrationAsync(registration);
                TempData["Message"] = "Thông tin đăng ký đã được cập nhật thành công!";
                return RedirectToAction(nameof(MyRegistrations));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật: {ex.Message}";
                model.EventTitle = (await _eventRepository.GetEventByIdAsync(model.EventId))?.Title ?? "N/A";
                return View(model);
            }
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Challenge();
            }

            var registrations = await _registrationRepository.GetRegistrationHistoryAsync(userId);
            var checkIns = await _context.CheckIns
                .Join(_context.Registrations,
                    c => c.RegistrationId,
                    r => r.Id,
                    (c, r) => new { CheckIn = c, Registration = r })
                .Where(x => x.Registration.UserId == userId && x.Registration.CancellationDate == null)
                .Select(x => x.CheckIn)
                .ToListAsync();

            var model = registrations.Select(r => new RegistrationHistoryViewModel
            {
                RegistrationId = r.Id,
                EventId = r.EventId,
                EventTitle = r.Event?.Title ?? "N/A",
                EventStartDate = r.Event?.StartDate ?? DateTime.MinValue,
                EventLocation = r.Event?.Location ?? "N/A",
                RegistrationDate = r.RegistrationDate,
                IsCancelled = r.CancellationDate.HasValue,
                CancellationReason = r.CancellationReason,
                HasCheckedIn = checkIns.Any(c => c.RegistrationId == r.Id),
                CheckInTime = checkIns.FirstOrDefault(c => c.RegistrationId == r.Id)?.CheckInTime
            }).ToList();

            return View(model);
        }
    }
}