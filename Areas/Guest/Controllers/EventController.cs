using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace QuickEvent.Areas.Guest.Controllers
{
    [Area("Guest")]
    [Authorize(Roles = "Guest,Organizer")]
    public class EventController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICalendarService _calendarService;
        private readonly IRegistrationRepository _registrationRepository;

        public EventController(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        ICalendarService calendarService)
        {
            _eventRepository = eventRepository;
            _registrationRepository = registrationRepository;
            _calendarService = calendarService;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _eventRepository.SearchEventsAsync("", true);
            return View(events);
        }

        public async Task<IActionResult> Details(int id)
        {
            var @event = await _eventRepository.GetEventByIdAsync(id);
            if (@event == null || !@event.IsPublic || @event.IsCancelled)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Registration registration = null;
            if (userId != null)
            {
                var registrations = await _registrationRepository.GetRegistrationsByUserAsync(userId);
                registration = registrations.FirstOrDefault(r => r.EventId == id);
            }

            var model = new QuickEvent.Areas.Guest.Models.EventDetailsViewModel
            {
                Event = @event,
                Registration = registration
            };

            return View(model);
        }

        public async Task<IActionResult> DownloadIcs(int id)
        {
            var @event = await _eventRepository.GetEventByIdAsync(id);
            if (@event == null || !@event.IsPublic || @event.IsCancelled)
            {
                return NotFound();
            }

            var icsContent = _calendarService.GenerateIcsContent(@event);
            var bytes = Encoding.UTF8.GetBytes(icsContent);
            var fileName = $"{@event.Title?.Replace(" ", "_") ?? "Event"}_event.ics";

            return File(bytes, "text/calendar", fileName);
        }
    }
}