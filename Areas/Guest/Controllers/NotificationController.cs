using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Areas.Guest.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Areas.Guest.Controllers
{
    [Area("Guest")]
    [Authorize(Roles = "Guest,Organizer")]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Challenge();
            }

            var notifications = await _notificationRepository.GetNotificationsByUserAsync(userId);
            var model = notifications.Select(n => new NotificationViewModel
            {
                Id = n.Id,
                Message = n.Message,
                CreatedDate = n.CreatedDate,
                IsRead = n.IsRead,
                EventTitle = n.Event?.Title ?? "N/A"
            }).ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Challenge();
            }

            await _notificationRepository.MarkAsReadAsync(notificationId);
            return RedirectToAction("Index");
        }
    }
}