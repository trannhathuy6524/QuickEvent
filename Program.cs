using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;
using QuickEvent.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấp phép EPPlus
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Cấu hình DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();

//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.ListenAnyIP(5005);
//});

// Cấu hình chính sách ủy quyền
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddRazorPages();

// Thêm các dịch vụ cho controller và view
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<QRCodeService>();

// Đăng ký các repository
builder.Services.AddScoped<IEventRepository, EFEventRepository>();
builder.Services.AddScoped<ICalendarService, EFCalendarService>();  
builder.Services.AddScoped<ICheckInRepository, EFCheckInRepository>();
builder.Services.AddScoped<IFormAccessRepository, EFFormAccessRepository>(); 
builder.Services.AddScoped<INotificationRepository, EFNotificationRepository>();
builder.Services.AddScoped<IRegistrationRepository, EFRegistrationRepository>();
builder.Services.AddScoped<IApplicationUserRepository, EFApplicationUserRepository>();
builder.Services.AddScoped<IOrganizerRequestRepository, EFOrganizerRequestRepository>();

var app = builder.Build();

// Cấu hình pipeline xử lý HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapRazorPages();
});

app.MapRazorPages();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed vai trò và người dùng
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleSeeder.SeedRolesAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi seed vai trò và người dùng admin.");
    }
}

app.Run();