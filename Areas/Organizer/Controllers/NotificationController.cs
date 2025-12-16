using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickEvent.Areas.Organizer.Models;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace QuickEvent.Areas.Organizer.Controllers
{
    [Area("Organizer")]
    [Authorize(Roles = "Organizer")]
    public class NotificationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEventRepository _eventRepository;
        private readonly ApplicationDbContext _context;

            public NotificationController(
            INotificationRepository notificationRepository,
            IEventRepository eventRepository,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _notificationRepository = notificationRepository;
            _eventRepository = eventRepository;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Challenge();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .GroupJoin(_context.Events,  // Sử dụng GroupJoin thay vì Join
                    n => n.EventId,
                    e => e.Id,
                    (n, e) => new { Notification = n, Events = e })
                .SelectMany(x => x.Events.DefaultIfEmpty(),
                    (x, e) => new NotificationViewModel
                    {
                        Id = x.Notification.Id,
                        EventId = x.Notification.EventId,
                        EventTitle = e != null ? e.Title : null,  // Xử lý trường hợp event null
                        Message = x.Notification.Message,
                        CreatedDate = x.Notification.CreatedDate,
                        IsRead = x.Notification.IsRead
                    })
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Challenge();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification == null)
            {
                TempData["Error"] = "Không tìm thấy thông báo.";
                return RedirectToAction("Index");
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            TempData["Message"] = "Đã đánh dấu thông báo là đã đọc.";
            return RedirectToAction("Index");
        }
    }
}