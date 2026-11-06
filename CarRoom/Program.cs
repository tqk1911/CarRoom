using CarRoom.Data;
using CarRoom.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. ĐĂNG KÝ SERVICES
// ==========================================

// MVC
builder.Services.AddControllersWithViews();

// HttpClient (dùng cho API, Chatbot...)
builder.Services.AddHttpClient();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
    )
);

// ==========================================
// 2. AUTHENTICATION - XÁC THỰC
// ==========================================

var auth = builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
)
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
})
.AddCookie("External");

// Cấu hình đăng nhập Google
// Chỉ kích hoạt khi đã có ClientId trong appsettings.json
var googleId = builder.Configuration[
    "Authentication:Google:ClientId"
];

if (!string.IsNullOrEmpty(googleId))
{
    auth.AddGoogle(options =>
    {
        options.ClientId = googleId;

        options.ClientSecret = builder.Configuration[
            "Authentication:Google:ClientSecret"
        ]!;

        options.SignInScheme = "External";
    });
}

// ==========================================
// 3. SESSION
// ==========================================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;
});

// ==========================================
// 4. BUILD APPLICATION
// ==========================================

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    string emailAdmin = "admin@carroom.com";

    bool daTonTai = await db.Users.AnyAsync(u => u.Email == emailAdmin);

    if (!daTonTai)
    {
        var admin = new User
        {
            HoTen = "Quản trị viên",
            Email = emailAdmin,
            MatKhau = BCrypt.Net.BCrypt.HashPassword("Admin@123456"),
            SoDienThoai = "0999999999",
            VaiTro = "Admin",
            TrangThai = true,
            NgayTao = DateTime.Now
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}

// ==========================================
// 5. HTTP REQUEST PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Session
app.UseSession();

// Authentication phải đứng trước Authorization
app.UseAuthentication();

app.UseAuthorization();

// ==========================================
// 6. ROUTING
// ==========================================

// Areas
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

// Static assets
app.MapStaticAssets();

// ==========================================
// 7. RUN APPLICATION
// ==========================================

app.Run();
