using System.Security.Claims;
using BCrypt.Net;
using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) => _db = db;
        // đăng ký
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home"); //ktra đăng nhập chưa
            return View();
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register (RegisterViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm); //ktra dữ liệu hle
            var email = vm.Email.Trim().ToLower();
            if(await _db .Users.AnyAsync(u =>u.Email == email)) 
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
            await SignInAsync(user, false);
            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("Index", "Home");
        }
        //đăng nhập
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if(User.Identity?.IsAuthenticated ==true) return RedirectToAction("Index", "Home");
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
            if(user==null||string.IsNullOrEmpty(user.MatKhau)||!BCrypt.Net.BCrypt.Verify(vm.MatKhau, user.MatKhau))
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
            if(!string.IsNullOrEmpty(returnUrl)&& Url.IsLocalUrl(returnUrl))
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

        //đăng xuất
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        //từ chối quyền
        public IActionResult Denied()
        {
            return View();
        }

        // tạo cookie
        private async Task SignInAsync(User user, bool ghiNho)
        {
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
                new AuthenticationProperties
                {
                    IsPersistent = ghiNho,
                    ExpiresUtc = ghiNho ? DateTimeOffset.UtcNow.AddDays(14) : null
                }); 
        }


    }
}
