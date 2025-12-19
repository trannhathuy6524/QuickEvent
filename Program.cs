using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using QuickEvent.Data;
using QuickEvent.Models;
using QuickEvent.Repositories;
using QuickEvent.Repositories.Interfaces;
using QuickEvent.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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


// JWT Configuration - Thêm JWT Bearer KHÔNG ghi đè Cookie Authentication mặc định
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuickEventAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "QuickEventMobile",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ??
                "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"))
        };

        // Cấu hình để trả về JSON thay vì redirect HTML cho API
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = "Bạn cần đăng nhập để truy cập tài nguyên này",
                    error = "Unauthorized"
                });
                return context.Response.WriteAsync(result);
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    message = "Bạn không có quyền truy cập tài nguyên này",
                    error = "Forbidden"
                });
                return context.Response.WriteAsync(result);
            }
        };
    });

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5217); // Listen trên tất cả IP addresses
});

// Cấu hình chính sách ủy quyền
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddRazorPages();

// Thêm các dịch vụ cho controller và view
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Cấu hình JSON serialization cho API
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true; // Format JSON đẹp hơn (optional)
    });

builder.Services.AddScoped<QRCodeService>();
builder.Services.AddSingleton<WebSocketHub>();

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

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120)
});

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

// Map API Controllers TRƯỚC (ưu tiên API routes)
app.MapControllers();

// ✅ WebSocket endpoint
app.Map("/api/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var userId = context.Request.Query["userId"].ToString();
    if (string.IsNullOrEmpty(userId))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("UserId is required");
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var webSocketHub = context.RequestServices.GetRequiredService<WebSocketHub>();

    await webSocketHub.HandleConnectionAsync(userId, webSocket);
});

// Map Razor Pages
app.MapRazorPages();

// Map Area Controllers (cho Razor Pages MVC trong Areas)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Map Default Controller Route
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