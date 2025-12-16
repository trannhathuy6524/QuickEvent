using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;
using QuickEvent.Services;

namespace QuickEvent.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class CheckInController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly ICheckInRepository _checkInRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QRCodeService _qrCodeService;

        public CheckInController(
            IEventRepository eventRepository,
            IRegistrationRepository registrationRepository,
            ICheckInRepository checkInRepository,
            UserManager<ApplicationUser> userManager,
            QRCodeService qrCodeService)
        {
            _eventRepository = eventRepository;
            _registrationRepository = registrationRepository;
            _checkInRepository = checkInRepository;
            _userManager = userManager;
            _qrCodeService = qrCodeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
                return NotFound();

            var registrations = await _registrationRepository.GetRegistrationsByEventAsync(eventId);
            var checkIns = await _checkInRepository.GetCheckInsByEventAsync(eventId);
            var checkInStatus = new Dictionary<int, bool>();
            foreach (var reg in registrations)
            {
                checkInStatus[reg.Id] = checkIns.Any(c => c.RegistrationId == reg.Id);
            }
            ViewBag.EventId = eventId;
            ViewBag.CheckInStatus = checkInStatus;
            return View(registrations);
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn(int registrationId, int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var @event = await _eventRepository.GetEventByIdAsync(eventId);
            if (@event == null || @event.OrganizerId != currentUser.Id)
                return NotFound();

            var registration = await _registrationRepository.GetRegistrationByIdAsync(registrationId);
            if (registration == null || registration.EventId != eventId)
                return NotFound();

            if (registration.CancellationDate != null)
            {
                TempData["Error"] = "Không thể check-in vì đăng ký đã bị hủy.";
                return RedirectToAction("Index", new { eventId });
            }

            var existingCheckIn = await _checkInRepository.GetCheckInByRegistrationId(registrationId);
            if (existingCheckIn != null)
            {
                TempData["Error"] = "Người dùng đã check-in trước đó.";
                return RedirectToAction("Index", new { eventId });
            }

            var checkIn = new CheckIn
            {
                RegistrationId = registrationId,
                EventId = eventId,
                CheckInTime = DateTime.Now
            };
            await _checkInRepository.AddCheckInAsync(checkIn);

            TempData["Message"] = "Check-in thành công!";
            return RedirectToAction("Index", new { eventId });
        }

        [HttpGet]
        public IActionResult QR_Check()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessQRCode([FromBody] string qrData)
        {
            var (isValid, registrationId, eventId) = _qrCodeService.ValidateQRCode(qrData);

            if (!isValid)
                return Json(new { success = false, message = "Mã QR không hợp lệ" });

            var registration = await _registrationRepository.GetRegistrationByIdAsync(registrationId);
            if (registration == null || registration.EventId != eventId)
                return Json(new { success = false, message = "Thông tin đăng ký không hợp lệ" });

            if (registration.CancellationDate != null)
                return Json(new { success = false, message = "Đăng ký đã bị hủy" });

            var existingCheckIn = await _checkInRepository.GetCheckInByRegistrationId(registrationId);
            if (existingCheckIn != null)
                return Json(new { success = false, message = "Người dùng đã check-in trước đó" });

            var checkIn = new CheckIn
            {
                RegistrationId = registrationId,
                EventId = eventId,
                CheckInTime = DateTime.Now
            };

            await _checkInRepository.AddCheckInAsync(checkIn);

            return Json(new
            {
                success = true,
                message = "Check-in thành công",
                data = new
                {
                    name = registration.FullName,
                    email = registration.Email,
                    checkInTime = checkIn.CheckInTime.ToString("dd/MM/yyyy HH:mm:ss")
                }
            });
        }
    }
}