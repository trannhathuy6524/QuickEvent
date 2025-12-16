using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private readonly IOrganizerRequestRepository _requestRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRepository _notificationRepository; // Thêm dependency này

        public HomeController(
            IEventRepository eventRepository,
            IOrganizerRequestRepository requestRepository,
            UserManager<ApplicationUser> userManager,
            INotificationRepository notificationRepository)
        {
            _eventRepository = eventRepository;
            _requestRepository = requestRepository;
            _userManager = userManager;
            _notificationRepository = notificationRepository;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _eventRepository.SearchEventsAsync(null, true); // Chỉ lấy sự kiện công khai
            return View(events);
        }

        public async Task<IActionResult> Search(string query, bool activeOnly = false)
        {
            var events = await _eventRepository.SearchEventsAsync(query, true);

            if (activeOnly)
            {
                var now = DateTime.Now;
                events = events.Where(e => !e.EndDate.HasValue || e.EndDate.Value >= now).ToList();
            }

            return View("Index", events);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> RequestOrganizer()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            if (await _userManager.IsInRoleAsync(user, "Organizer"))
            {
                TempData["Message"] = "Bạn đã là Organizer!";
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestOrganizer(string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "Identity" });

            // Kiểm tra yêu cầu trùng lặp
            var existingRequest = await _requestRepository.GetPendingRequestByUserIdAsync(user.Id);
            if (existingRequest != null)
            {
                TempData["Error"] = "Bạn đã có một yêu cầu đang chờ phê duyệt!";
                return View();
            }

            // Kiểm tra role hiện tại
            if (await _userManager.IsInRoleAsync(user, "Organizer"))
            {
                TempData["Error"] = "Bạn đã là Organizer!";
                return RedirectToAction("Index");
            }

            // Tạo yêu cầu mới
            var request = new OrganizerRequest
            {
                UserId = user.Id,
                Reason = reason,
                Status = "Chờ phê duyệt",
                RequestDate = DateTime.Now
            };
            await _requestRepository.AddRequestAsync(request);

            // Gửi thông báo cho tất cả Admin
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                var notification = new Notification
                {
                    UserId = admin.Id,
                    Message = $"Người dùng {user.FullName} ({user.Email}) đã yêu cầu trở thành Organizer.",
                    CreatedDate = DateTime.Now,
                    IsRead = false,
                    Type = "OrganizerRequest",
                };
                await _notificationRepository.AddNotificationAsync(notification);
            }

            TempData["Message"] = "Yêu cầu trở thành Organizer của bạn đã được gửi!";
            return RedirectToAction("Index");
        }
    }
}