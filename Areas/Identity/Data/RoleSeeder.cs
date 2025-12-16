using Microsoft.AspNetCore.Identity;
using QuickEvent.Models;

namespace QuickEvent.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Admin", "Guest", "Organizer" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create default Admin if not exists
            var adminEmail = "admin123@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    UserType = "Admin",
                    IsApproved = true,
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123@");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create default Organizer if not exists
            var organizerEmail = "organizer123@gmail.com";
            var organizerUser = await userManager.FindByEmailAsync(organizerEmail);

            if (organizerUser == null)
            {
                organizerUser = new ApplicationUser
                {
                    UserName = organizerEmail,
                    Email = organizerEmail,
                    EmailConfirmed = true,
                    FullName = "Event Organizer",
                    UserType = "Organizer",
                    IsApproved = true,
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(organizerUser, "Organizer123@");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(organizerUser, "Organizer");
                }
            }
        }
    }
}