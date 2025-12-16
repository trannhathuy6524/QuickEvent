using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Areas.Admin.Models;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly INotificationRepository _notificationRepository;

        public UserManagementController(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            INotificationRepository notificationRepository)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _notificationRepository = notificationRepository;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepository.GetAllUsersAsync();
            var viewModel = users.Select(u => new UserManagementViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                UserType = u.UserType,
                RegistrationDate = u.RegistrationDate,
                IsLocked = u.LockoutEnd > DateTimeOffset.Now,
                IsApproved = u.IsApproved,
                Roles = _userManager.GetRolesAsync(u).Result.ToList()
            }).ToList();
            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            var viewModel = new UserManagementViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                RegistrationDate = user.RegistrationDate,
                IsLocked = user.LockoutEnd > DateTimeOffset.Now,
                IsApproved = user.IsApproved,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Block(string id)
        {
            await _userRepository.BlockUserAsync(id);
            TempData["Message"] = "Tài khoản đã bị khóa.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Unblock(string id)
        {
            await _userRepository.UnblockUserAsync(id);
            TempData["Message"] = "Tài khoản đã được mở khóa.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            // Lấy role hiện tại của user
            var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Guest";

            var viewModel = new UserManagementViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType,
                SelectedRole = currentRole // Gán role hiện tại vào SelectedRole
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserManagementViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Bỏ qua lỗi cho UserType
                    ModelState.Remove("UserType");
                    if (!ModelState.IsValid)
                    {
                        var errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage);
                        TempData["Error"] = string.Join(", ", errors);
                        return View(model);
                    }
                }

                var user = await _userRepository.GetUserByIdAsync(model.Id);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy người dùng.";
                    return RedirectToAction("Index");
                }

                // Kiểm tra xem có phải admin không
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Contains("Admin"))
                {
                    TempData["Error"] = "Không thể thay đổi quyền của tài khoản Admin.";
                    return RedirectToAction("Index");
                }

                // Cập nhật thông tin cơ bản
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;

                // Xử lý thay đổi role và UserType
                if (!string.IsNullOrEmpty(model.SelectedRole))
                {
                    // Xóa tất cả role hiện tại (trừ Admin)
                    foreach (var role in currentRoles.Where(r => r != "Admin"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }

                    // Thêm role mới và cập nhật UserType
                    await _userManager.AddToRoleAsync(user, model.SelectedRole);
                    user.UserType = model.SelectedRole; // Gán UserType bằng SelectedRole
                    user.IsApproved = model.SelectedRole == "Organizer";
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    if (currentRoles.FirstOrDefault() != model.SelectedRole)
                    {
                        await _notificationRepository.AddNotificationAsync(new Notification
                        {
                            UserId = user.Id,
                            Message = $"Quyền của bạn đã được thay đổi thành {model.SelectedRole}",
                            CreatedDate = DateTime.Now,
                            IsRead = false,
                            Type = "RoleChanged"
                        });
                    }

                    TempData["Message"] = "Cập nhật thông tin thành công.";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();
            var viewModel = new ResetPasswordViewModel
            {
                UserId = user.Id
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userRepository.GetUserByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (result.Succeeded)
            {
                TempData["Message"] = "Reset mật khẩu thành công.";
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }
    }
}