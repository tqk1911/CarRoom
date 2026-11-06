using System.Security.Claims;
using CarRoom.Data;
using CarRoom.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Controllers
{
    public class GoogleAuthController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IAuthenticationSchemeProvider _schemes;

        public GoogleAuthController(AppDbContext db, IAuthenticationSchemeProvider schemes)
        {
            _db = db;
            _schemes = schemes;
        }

        // Nút "Đăng nhập bằng Google" trỏ về đây
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (await _schemes.GetSchemeAsync(GoogleDefaults.AuthenticationScheme) == null)
            {
                TempData["Error"] = "Đăng nhập Google chưa được cấu hình trên máy này.";
                return RedirectToAction("Login", "Account");
            }

            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), new { returnUrl })
            };
            return Challenge(props, GoogleDefaults.AuthenticationScheme);
        }

        // Google gọi lại đây sau khi người dùng đồng ý
        public async Task<IActionResult> Callback(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync("External");
            if (!result.Succeeded || result.Principal == null)
                return RedirectToAction("Login", "Account");

            var email = result.Principal.FindFirstValue(ClaimTypes.Email)?.Trim().ToLower();
            var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
            var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            await HttpContext.SignOutAsync("External");   // bỏ cookie tạm

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Google không trả về email.";
                return RedirectToAction("Login", "Account");
            }

            var user = await FindOrCreateUserAsync(_db, email, name!, googleId);

            if (!user.TrangThai)
            {
                TempData["Error"] = "Tài khoản đã bị khóa.";
                return RedirectToAction("Login", "Account");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.HoTen),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.VaiTro)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return user.VaiTro switch
            {
                "Admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
                "Staff" => RedirectToAction("Index", "Appointments", new { area = "Admin" }),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // Tách riêng để viết unit test được
        public static async Task<User> FindOrCreateUserAsync(AppDbContext db, string email, string name, string? googleId)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                user = new User
                {
                    HoTen = name,
                    Email = email,
                    GoogleId = googleId,
                    VaiTro = "Customer",
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };
                db.Users.Add(user);
            }
            else if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleId;   // email đã đăng ký thường: gộp vào cùng tài khoản
            }
            await db.SaveChangesAsync();
            return user;
        }
    }
}