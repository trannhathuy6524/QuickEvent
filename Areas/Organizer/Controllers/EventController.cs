    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using QuickEvent.Areas.Organizer.Models;
    using QuickEvent.Data;
    using QuickEvent.Models;
    using QuickEvent.Repositories.Interfaces;

    namespace QuickEvent.Areas.Organizer.Controllers
    {
        [Area("Organizer")]
        [Authorize(Roles = "Organizer")]
        public class EventController : Controller
        {
            private readonly IEventRepository _eventRepository;
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly ApplicationDbContext _context;

            public EventController(IEventRepository eventRepository,
                UserManager<ApplicationUser> userManager,
                ApplicationDbContext context)
            {
                _eventRepository = eventRepository;
                _userManager = userManager;
                _context = context;
            }

            public async Task<IActionResult> Index()
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                }
                var events = await _eventRepository.GetEventsByOrganizerAsync(currentUser.Id);
                foreach (var eventItem in events)
                {
                    await _eventRepository.UpdateEventStatusAsync(eventItem.Id);
                }
                return View(events);
            }

            public IActionResult Create()
            {
                return View();
            }

            [HttpPost]
            public async Task<IActionResult> Create(EventCreateViewModel model)
            {
                if (ModelState.IsValid)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        ModelState.AddModelError("", "Không tìm thấy người dùng hiện tại. Vui lòng đăng nhập lại.");
                        return View(model);
                    }

                    var isOrganizer = await _userManager.IsInRoleAsync(currentUser, "Organizer");
                    if (!isOrganizer)
                    {
                        ModelState.AddModelError("", "Người dùng không có quyền Organizer để tạo sự kiện.");
                        return View(model);
                    }

                    var eventItem = new Event
                    {
                        Title = model.Title,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        Description = model.Description,
                        Location = model.Location,
                        MaxAttendees = model.MaxAttendees,
                        IsPublic = model.IsPublic,
                        OrganizerId = currentUser.Id,
                        IsRegistrationOpen = true,
                        Status = "Mở"
                    };
                    await _eventRepository.AddEventAsync(eventItem);
                    return RedirectToAction("Index");
                }
                return View(model);
            }

            public async Task<IActionResult> Details(int id)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != currentUser.Id)
                {
                    return NotFound("Không tìm thấy sự kiện hoặc bạn không có quyền xem.");
                }
                return View(eventItem);
            }

            public async Task<IActionResult> Edit(int id)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != currentUser.Id)
                    return NotFound();
                if (eventItem.StartDate <= DateTime.Now)
                {
                    TempData["Error"] = "Không thể chỉnh sửa sự kiện đã bắt đầu!";
                    return RedirectToAction("Index");
                }
                var model = new EventEditViewModel
                {
                    Id = eventItem.Id,
                    Title = eventItem.Title,
                    StartDate = eventItem.StartDate,
                    EndDate = eventItem.EndDate,
                    Description = eventItem.Description,
                    Location = eventItem.Location,
                    MaxAttendees = eventItem.MaxAttendees,
                    IsPublic = eventItem.IsPublic,
                    IsRegistrationOpen = eventItem.IsRegistrationOpen,
                    Status = eventItem.Status
                };
                return View(model);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(int id, EventEditViewModel model)
            {
                if (ModelState.IsValid)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        return NotFound("Không tìm thấy người dùng hiện tại.");
                    }

                    var eventItem = await _eventRepository.GetEventByIdAsync(id);
                    if (eventItem == null || eventItem.OrganizerId != currentUser.Id)
                        return NotFound();
                    if (eventItem.StartDate <= DateTime.Now)
                    {
                        TempData["Error"] = "Không thể chỉnh sửa sự kiện đã bắt đầu!";
                        return RedirectToAction("Index");
                    }
                    eventItem.Title = model.Title;
                    eventItem.StartDate = model.StartDate;
                    eventItem.EndDate = model.EndDate;
                    eventItem.Description = model.Description;
                    eventItem.Location = model.Location;
                    eventItem.MaxAttendees = model.MaxAttendees;
                    eventItem.IsPublic = model.IsPublic;
                    eventItem.IsRegistrationOpen = model.IsRegistrationOpen;
                    eventItem.Status = model.Status;
                    await _eventRepository.UpdateEventAsync(eventItem);
                    await _eventRepository.UpdateEventStatusAsync(id);
                    TempData["Message"] = "Sự kiện đã được cập nhật. Thông báo đã được gửi đến người đăng ký.";
                    return RedirectToAction("Index");
                }
                return View(model);
            }

            [HttpPost]
            public async Task<IActionResult> ToggleRegistration(int id)
            {
                try
                {
                    // Kiểm tra người dùng hiện tại
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser == null)
                    {
                        TempData["Error"] = "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.";
                        return RedirectToAction("Index");
                    }

                    // Kiểm tra sự kiện tồn tại và quyền truy cập
                    var eventItem = await _eventRepository.GetEventByIdAsync(id);
                    if (eventItem == null)
                    {
                        TempData["Error"] = "Không tìm thấy sự kiện.";
                        return RedirectToAction("Index");
                    }

                    if (eventItem.OrganizerId != currentUser.Id)
                    {
                        TempData["Error"] = "Bạn không có quyền thay đổi trạng thái đăng ký của sự kiện này.";
                        return RedirectToAction("Index");
                    }

                    // Kiểm tra các điều kiện không cho phép thay đổi
                    if (eventItem.IsCancelled)
                    {
                        TempData["Error"] = "Không thể thay đổi trạng thái đăng ký của sự kiện đã bị hủy.";
                        return RedirectToAction("Index");
                    }

                    if (eventItem.StartDate <= DateTime.Now)
                    {
                        TempData["Error"] = "Không thể thay đổi trạng thái đăng ký của sự kiện đã bắt đầu.";
                        return RedirectToAction("Index");
                    }

                    // Thực hiện thay đổi trạng thái
                    eventItem.IsRegistrationOpen = !eventItem.IsRegistrationOpen;
                    var status = eventItem.IsRegistrationOpen ? "mở" : "đóng";

                    // Tạo thông báo cho tất cả người đăng ký
                    var registrations = await _eventRepository.GetEventByIdAsync(id);
                    if (registrations != null && registrations.Registrations != null)
                    {
                        foreach (var registration in registrations.Registrations.Where(r => r.CancellationDate == null))
                        {
                            var notification = new Notification
                            {
                                UserId = registration.UserId,
                                Message = $"Trạng thái đăng ký của sự kiện '{eventItem.Title}' đã thay đổi. Hiện tại đã {status} đăng ký.",
                                EventId = eventItem.Id,
                                CreatedDate = DateTime.Now,
                                Type = "RegistrationStatusChanged"
                            };
                            _context.Notifications.Add(notification);
                        }
                    }

                    // Cập nhật sự kiện
                    await _eventRepository.UpdateEventAsync(eventItem);
                    await _eventRepository.UpdateEventStatusAsync(id);

                    await _context.SaveChangesAsync(); // Add this line to save notification changes
                    return RedirectToAction("Index");

                    // Thông báo thành công
                    TempData["Message"] = $"Đã {status} đăng ký sự kiện '{eventItem.Title}'. Thông báo đã được gửi đến người đăng ký.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Đã xảy ra lỗi khi thay đổi trạng thái đăng ký: {ex.Message}";
                    return RedirectToAction("Index");
                }
            }

            public async Task<IActionResult> Cancel(int id)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != currentUser.Id)
                    return NotFound();
                return View(eventItem);
            }

            [HttpPost, ActionName("Cancel")]
            public async Task<IActionResult> CancelConfirmed(int id)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != currentUser.Id)
                    return NotFound();
                await _eventRepository.CancelEventAsync(id);
                TempData["Message"] = "Sự kiện đã bị hủy. Thông báo đã được gửi đến người đăng ký.";
                return RedirectToAction("Index");
            }

            public async Task<IActionResult> Delete(int id)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != currentUser.Id)
                    return NotFound();
                return View(eventItem);
            }

            [HttpPost, ActionName("Delete")]
            public async Task<IActionResult> DeleteConfirmed(int id)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return NotFound("Không tìm thấy người dùng hiện tại.");
                }

                var eventItem = await _eventRepository.GetEventByIdAsync(id);
                if (eventItem == null || eventItem.OrganizerId != currentUser.Id)
                    return NotFound();
                await _eventRepository.DeleteEventAsync(id);
                TempData["Message"] = "Sự kiện đã bị xóa.";
                return RedirectToAction("Index");
            }
        }
    }