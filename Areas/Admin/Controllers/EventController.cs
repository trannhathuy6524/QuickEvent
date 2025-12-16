using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "Admin")]
    public class EventController : Controller
    {
        private readonly IEventRepository _eventRepository;

        public EventController(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _eventRepository.SearchEventsAsync(null, false); // Lấy tất cả sự kiện
            return View(events);
        }

        public async Task<IActionResult> Details(int id)
        {
            var @event = await _eventRepository.GetEventByIdAsync(id);
            if (@event == null)
                return NotFound();
            return View(@event);
        }

        public async Task<IActionResult> Approve(int id)
        {
            var @event = await _eventRepository.GetEventByIdAsync(id);
            if (@event == null)
                return NotFound();
            // Giả sử có thuộc tính Status trong Event để duyệt
            @event.IsPublic = true; // Coi như duyệt bằng cách công khai sự kiện
            await _eventRepository.UpdateEventAsync(@event);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            var @event = await _eventRepository.GetEventByIdAsync(id);
            if (@event == null)
                return NotFound();
            return View(@event);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _eventRepository.DeleteEventAsync(id);
            return RedirectToAction("Index");
        }
    }
}