using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories.Interfaces;

namespace QuickEvent.Repositories
{
    public class EFEventRepository : IEventRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EFEventRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task AddEventAsync(Event @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event), "Sự kiện không thể là null.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == @event.OrganizerId);
            if (!userExists)
            {
                throw new InvalidOperationException($"Không tìm thấy người dùng với Id: {@event.OrganizerId} trong bảng AspNetUsers.");
            }

            _context.Events.Add(@event);
            await _context.SaveChangesAsync();
        }

        public async Task<Event> GetEventByIdAsync(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (@event == null)
            {
                Console.WriteLine($"Không tìm thấy sự kiện với {id}.");
            }
            return @event;
        }

        public async Task<List<Event>> GetEventsByOrganizerAsync(string organizerId)
        {
            return await _context.Events
                .Include(e => e.Registrations)
                .Where(e => e.OrganizerId == organizerId && !e.IsCancelled)
                .ToListAsync();
        }

        public async Task<List<Event>> SearchEventsAsync(string query, bool onlyPublic)
        {
            var events = _context.Events.AsQueryable();
            if (!string.IsNullOrEmpty(query))
            {
                events = events.Where(e => e.Title.Contains(query) || e.Description.Contains(query));
            }
            if (onlyPublic)
            {
                events = events.Where(e => e.IsPublic && !e.IsCancelled);
            }
            return await events.ToListAsync();
        }

        public async Task UpdateEventAsync(Event @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event), "Sự kiện không thể là null.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == @event.OrganizerId);
            if (!userExists)
            {
                throw new InvalidOperationException($"Không tìm thấy người dùng với Id: {@event.OrganizerId} trong bảng AspNetUsers.");
            }

            var existingEvent = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == @event.Id);
            if (existingEvent == null)
            {
                throw new InvalidOperationException($"Không tìm thấy sự kiện với Id: {@event.Id}.");
            }

            _context.Entry(existingEvent).CurrentValues.SetValues(@event);
            await _context.SaveChangesAsync();

            var message = $"Sự kiện '{@event.Title}' đã được cập nhật. Thời gian: {@event.StartDate:dd/MM/yyyy HH:mm}, Địa điểm: {@event.Location}, Chi tiết: {@event.Description}";
            foreach (var registration in existingEvent.Registrations.Where(r => r.CancellationDate == null))
            {
                var user = await _userManager.FindByIdAsync(registration.UserId);
                if (user != null)
                {
                    var isOrganizer = await _userManager.IsInRoleAsync(user, "Organizer");
                    if (!isOrganizer)
                    {
                        var notification = new Notification
                        {
                            UserId = registration.UserId,
                            Message = message,
                            EventId = @event.Id,
                            CreatedDate = DateTime.Now,
                            Type = "EventUpdated" // Thêm Type
                        };
                        _context.Notifications.Add(notification);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task CancelEventAsync(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (@event != null)
            {
                @event.IsCancelled = true;
                await _context.SaveChangesAsync();

                var message = $"Sự kiện '{@event.Title}' đã bị hủy.";
                foreach (var registration in @event.Registrations.Where(r => r.CancellationDate == null))
                {
                    var user = await _userManager.FindByIdAsync(registration.UserId);
                    if (user != null)
                    {
                        var isOrganizer = await _userManager.IsInRoleAsync(user, "Organizer");
                        if (!isOrganizer)
                        {
                            var notification = new Notification
                            {
                                UserId = registration.UserId,
                                Message = message,
                                EventId = @event.Id,
                                CreatedDate = DateTime.Now,
                                Type = "EventCancelled" // Thêm Type
                            };
                            _context.Notifications.Add(notification);
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteEventAsync(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetRegistrationCountAsync(int eventId)
        {
            return await _context.Registrations.CountAsync(r => r.EventId == eventId && r.CancellationDate == null);
        }

        public async Task UpdateEventStatusAsync(int eventId)
        {
            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);
            if (@event == null) return;

            var now = DateTime.Now;

            if (@event.EndDate.HasValue && @event.EndDate.Value < now)
            {
                @event.Status = "Đã đóng";
                @event.IsRegistrationOpen = false;
            }
            else if (@event.StartDate > now)
            {
                if (@event.StartDate <= now.AddDays(7))
                {
                    @event.Status = "Sắp diễn ra";
                }
                else
                {
                    @event.Status = "Đang mở";
                }
            }
            else
            {
                @event.Status = "Đang diễn ra";
            }

            var registrationCount = @event.Registrations?.Count(r => r.CancellationDate == null) ?? 0;
            if (registrationCount >= @event.MaxAttendees)
            {
                @event.IsRegistrationOpen = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<DateTime, int>> GetRegistrationsByDayAsync(int eventId, DateTime startDate, DateTime endDate)
        {
            var registrations = await _context.Registrations
                .Where(r => r.EventId == eventId && r.RegistrationDate >= startDate && r.RegistrationDate <= endDate && r.CancellationDate == null)
                .GroupBy(r => r.RegistrationDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            Console.WriteLine($"GetRegistrationsByDayAsync: EventId={eventId}, StartDate={startDate}, EndDate={endDate}, Found {registrations.Count} days");

            var result = new Dictionary<DateTime, int>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var count = registrations.FirstOrDefault(r => r.Date == date)?.Count ?? 0;
                result.Add(date, count);
            }

            return result;
        }

        public async Task<Dictionary<DateTime, int>> GetRegistrationsByWeekAsync(int eventId, DateTime startDate, DateTime endDate)
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            startDate = TimeZoneInfo.ConvertTime(startDate, timeZoneInfo);
            endDate = TimeZoneInfo.ConvertTime(endDate, timeZoneInfo);

            var registrations = await _context.Registrations
                .Where(r => r.EventId == eventId && r.RegistrationDate >= startDate && r.RegistrationDate <= endDate && r.CancellationDate == null)
                .ToListAsync();

            var weeklyRegistrations = registrations
                .GroupBy(r => r.RegistrationDate.Date.AddDays(-(int)r.RegistrationDate.DayOfWeek + (int)DayOfWeek.Monday))
                .Select(g => new { WeekStart = g.Key, Count = g.Count() })
                .ToList();

            Console.WriteLine($"GetRegistrationsByWeekAsync: EventId={eventId}, StartDate={startDate}, EndDate={endDate}, Found {weeklyRegistrations.Count} weeks");

            var result = new Dictionary<DateTime, int>();
            var firstWeekStart = startDate.Date.AddDays(-(int)startDate.DayOfWeek + (int)DayOfWeek.Monday);
            for (var weekStart = firstWeekStart; weekStart <= endDate.Date; weekStart = weekStart.AddDays(7))
            {
                var count = weeklyRegistrations.FirstOrDefault(r => r.WeekStart == weekStart)?.Count ?? 0;
                result.Add(weekStart, count);
            }

            return result;
        }
    }
}