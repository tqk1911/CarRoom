using System.Security.Claims;
using BCrypt.Net;
using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CarRoom.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db) => _db = db;

        // Đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim().ToLower();

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được đăng ký!");
                return View(vm);
            }

            var user = new User
            {
                HoTen = vm.HoTen.Trim(),
                Email = email,
                SoDienThoai = vm.SoDienThoai.Trim(),
                MatKhau = BCrypt.Net.BCrypt.HashPassword(vm.MatKhau),
                VaiTro = "Customer",
                TrangThai = true,
                NgayTao = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";

            return RedirectToAction("Login", "Account");
        }

        // Đăng nhập
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid) return View(vm);

            var email = vm.Email.Trim().ToLower();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null ||
                string.IsNullOrEmpty(user.MatKhau) ||
                !BCrypt.Net.BCrypt.Verify(vm.MatKhau, user.MatKhau))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng!");
                return View(vm);
            }

            if (!user.TrangThai)
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa");
                return View(vm);
            }

            await SignInAsync(user, vm.GhiNho);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return user.VaiTro switch
            {
                "Admin" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
                "Staff" => RedirectToAction("Index", "Appointments", new { area = "Admin" }),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // Đăng xuất
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        // Từ chối quyền
        public IActionResult Denied()
        {
            return View();
        }

        // Lấy ID người dùng đang đăng nhập
        private int GetUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        // Xem thông tin cá nhân
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var id = GetUserId();

            var user = await _db.Users.FindAsync(id);

            if (user == null)
                return RedirectToAction(nameof(Logout));

            var vm = new ProfileViewModel
            {
                HoTen = user.HoTen,
                Email = user.Email,
                SoDienThoai = user.SoDienThoai,

                Appointments = await _db.Appointments
                    .Include(a => a.Car)
                        .ThenInclude(c => c!.CarModel)
                        .ThenInclude(m => m!.Brand)
                    .Where(a => a.UserId == id)
                    .OrderByDescending(a => a.NgayHen)
                    .ToListAsync(),

                Favorites = await _db.Favorites
                    .Include(f => f.Car)
                        .ThenInclude(c => c!.CarModel)
                        .ThenInclude(m => m!.Brand)
                    .Include(f => f.Car)
                        .ThenInclude(c => c!.Colors)
                        .ThenInclude(col => col.CarImages)
                    .Where(f => f.UserId == id)
                    .OrderByDescending(f => f.NgayThem)
                    .ToListAsync()
            };

            return View(vm);
        }

        // Cập nhật thông tin cá nhân
        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateProfileInput input)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .First().ErrorMessage;

                return RedirectToAction(nameof(Profile));
            }

            var user = await _db.Users.FindAsync(GetUserId());

            if (user == null)
                return RedirectToAction(nameof(Logout));

            user.HoTen = input.HoTen;
            user.SoDienThoai = input.SoDienThoai;

            await _db.SaveChangesAsync();

            // Cập nhật lại cookie để tên hiển thị trên menu thay đổi
            await SignInAsync(user, false);

            TempData["Msg"] = "Đã lưu thay đổi.";

            return RedirectToAction(nameof(Profile));
        }

        // Tạo cookie đăng nhập
        private async Task SignInAsync(User user, bool ghiNho)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.HoTen),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.VaiTro)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = ghiNho,
                    ExpiresUtc = ghiNho
                        ? DateTimeOffset.UtcNow.AddDays(14)
                        : null
                });
        }
    }
}