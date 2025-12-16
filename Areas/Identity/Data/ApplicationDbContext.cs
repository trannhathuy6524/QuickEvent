using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuickEvent.Models;

namespace QuickEvent.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<OrganizerRequest> OrganizerRequests { get; set; }
        public DbSet<CheckIn> CheckIns { get; set; }
        public DbSet<FormAccess> FormAccesses { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Event-Organizer relationship
            builder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany()
                .HasForeignKey(e => e.OrganizerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Registration-Event relationship
            builder.Entity<Registration>()
                .HasOne(r => r.Event)
                .WithMany(e => e.Registrations) // Liên kết với collection Registrations trong Event
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Registration-User relationship
            builder.Entity<Registration>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure CheckIn-Registration relationship
            builder.Entity<CheckIn>()
                .HasOne(c => c.Registration)
                .WithMany()
                .HasForeignKey(c => c.RegistrationId)
                .OnDelete(DeleteBehavior.NoAction); // Thay đổi thành NoAction

            // Configure CheckIn-Event relationship
            builder.Entity<CheckIn>()
                .HasOne(c => c.Event)
                .WithMany()
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.NoAction); // Thay đổi thành NoAction
        }
    }
}