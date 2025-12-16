using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickEvent.Areas.Admin.Models;
using QuickEvent.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace QuickEvent.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IEventRepository _eventRepository;
        private readonly IRegistrationRepository _registrationRepository;
        private readonly IApplicationUserRepository _userRepository;
        private readonly IOrganizerRequestRepository _organizerRequestRepository;

        public DashboardController(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IApplicationUserRepository userRepository,
        IOrganizerRequestRepository organizerRequestRepository)
        {
            _eventRepository = eventRepository;
            _registrationRepository = registrationRepository;
            _userRepository = userRepository;
            _organizerRequestRepository = organizerRequestRepository;
        }

        public async Task<IActionResult> Index()
        {
            var allEvents = await _eventRepository.SearchEventsAsync(string.Empty, false);
            var allRegistrations = await _registrationRepository.GetRegistrationsByEventAsync(0);
            var allUsers = await _userRepository.GetAllUsersAsync();
            var allOrganizerRequests = await _organizerRequestRepository.GetPendingRequestsAsync();

            var now = DateTime.Now;
            var model = new StatisticsViewModel
            {
                TotalEvents = allEvents.Count,
                TotalRegistrations = allRegistrations.Count,
                ActiveUsers = allUsers.Count(u => u.LockoutEnd == null || u.LockoutEnd < DateTimeOffset.Now),
                PendingEvents = allEvents.Count(e => e.Status == "Chờ duyệt"),
                UpcomingEvents = allEvents.Count(e => e.StartDate > now),
                OngoingEvents = allEvents.Count(e => e.StartDate <= now && e.EndDate >= now),
                PendingOrganizerRequests = allOrganizerRequests.Count(r => r.Status == "Chờ duyệt"),
                NewUsersLast7Days = allUsers.Count(u => u.RegistrationDate >= now.AddDays(-7)),
                NewRegistrationsLast24Hours = allRegistrations.Count(r => r.RegistrationDate >= now.AddHours(-24))
            };
            return View(model);
        }

        public async Task<IActionResult> TotalRegistrations()
        {
            var allRegistrations = await _registrationRepository.GetRegistrationsByEventAsync(0);
            var now = DateTime.Now;
            var model = new StatisticsViewModel
            {
                TotalRegistrations = allRegistrations.Count,
                NewRegistrationsLast24Hours = allRegistrations.Count(r => r.RegistrationDate >= now.AddHours(-24))
            };
            return View(model);
        }
    }
}