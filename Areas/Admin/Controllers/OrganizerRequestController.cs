using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace QuickEvent.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrganizerRequestController : Controller
    {
        private readonly IOrganizerRequestRepository _requestRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRepository _notificationRepository;

        public OrganizerRequestController(
            IOrganizerRequestRepository requestRepository,
            UserManager<ApplicationUser> userManager,
            INotificationRepository notificationRepository)
        {
            _requestRepository = requestRepository;
            _userManager = userManager;
            _notificationRepository = notificationRepository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var requests = await _requestRepository.GetPendingRequestsAsync();
                if (!requests.Any())
                {
                    TempData["Message"] = "Không có yêu cầu chờ duyệt nào.";
                }
                return View(requests);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải danh sách yêu cầu: {ex.Message}";
                return View(new List<OrganizerRequest>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _requestRepository.GetRequestByIdAsync(id);
            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _requestRepository.GetRequestByIdAsync(id);
            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                // Cập nhật trạng thái yêu cầu
                request.Status = "Chấp nhận";
                await _requestRepository.UpdateRequestAsync(request);

                // Cập nhật role người dùng
                if (await _userManager.IsInRoleAsync(user, "Guest"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Guest");
                }
                if (!await _userManager.IsInRoleAsync(user, "Organizer"))
                {
                    await _userManager.AddToRoleAsync(user, "Organizer");
                }

                // Cập nhật thông tin người dùng
                user.IsApproved = true;
                user.UserType = "Organizer";
                await _userManager.UpdateAsync(user);

                // Gửi thông báo
                await _notificationRepository.AddNotificationAsync(new Notification
                {
                    UserId = user.Id,
                    Message = "Yêu cầu trở thành Organizer của bạn đã được chấp nhận!",
                    CreatedDate = DateTime.Now,
                    IsRead = false,
                    Type = "OrganizerRequestApproved"
                });

                TempData["Message"] = "Đã phê duyệt yêu cầu thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi phê duyệt yêu cầu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _requestRepository.GetRequestByIdAsync(id);
            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy người dùng.";
                    return RedirectToAction(nameof(Index));
                }

                request.Status = "Từ chối";
                await _requestRepository.UpdateRequestAsync(request);

                // Gửi thông báo
                await _notificationRepository.AddNotificationAsync(new Notification
                {
                    UserId = user.Id,
                    Message = "Yêu cầu trở thành Organizer của bạn đã bị từ chối.",
                    CreatedDate = DateTime.Now,
                    IsRead = false,
                    Type = "OrganizerRequestRejected"
                });

                TempData["Message"] = "Đã từ chối yêu cầu!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi từ chối yêu cầu: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}