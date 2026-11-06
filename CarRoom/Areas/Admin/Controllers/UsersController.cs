using System.Security.Claims;
using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _db;
        private const int PageSize = 10;
        private static readonly string[] AllRoles = { "Customer", "Staff", "Admin" };

        public UsersController(AppDbContext db) => _db = db;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // true nếu u là Admin đang hoạt động duy nhất còn lại
        private async Task<bool> IsLastActiveAdmin(User u) =>
            u.VaiTro == "Admin" && u.TrangThai &&
            !await _db.Users.AnyAsync(x => x.VaiTro == "Admin" && x.TrangThai && x.Id != u.Id);

        // ---------- Danh sách: ?role=Customer | Staff | Admin ----------
        public async Task<IActionResult> Index(string role = "Customer", string? q = null, int page = 1)
        {
            if (!AllRoles.Contains(role)) role = "Customer";
            if (page < 1) page = 1;

            var query = _db.Users.AsNoTracking().Where(u => u.VaiTro == role);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u => u.HoTen.Contains(q) || u.Email.Contains(q));
            }

            var result = new PagedResult<User>
            {
                Page = page,
                PageSize = PageSize,
                TotalItems = await query.CountAsync(),
                Items = await query.OrderByDescending(u => u.NgayTao)
                                   .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync()
            };

            ViewBag.Role = role;
            ViewBag.Q = q;
            ViewBag.CurrentUserId = CurrentUserId;
            return View(result);
        }

        // ---------- Thêm ----------
        public IActionResult Create(string role = "Customer")
        {
            if (!AllRoles.Contains(role)) role = "Customer";
            return View("Form", new UserFormViewModel { VaiTro = role });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            // Chuẩn hóa giống lúc đăng nhập (AccountController cũng Trim và ToLower email)
            var email = (model.Email ?? "").Trim().ToLower();

            if (string.IsNullOrEmpty(model.MatKhau))
                ModelState.AddModelError(nameof(model.MatKhau), "Vui lòng nhập mật khẩu");
            if (await _db.Users.AnyAsync(u => u.Email == email))
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng");
            if (!AllRoles.Contains(model.VaiTro))
                ModelState.AddModelError(nameof(model.VaiTro), "Vai trò không hợp lệ");

            if (!ModelState.IsValid) return View("Form", model);

            _db.Users.Add(new User
            {
                HoTen = model.HoTen.Trim(),
                Email = email,
                SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim(),
                VaiTro = model.VaiTro,
                TrangThai = true,
                NgayTao = DateTime.Now,
                MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau)
            });
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Đã thêm tài khoản.";
            return RedirectToAction(nameof(Index), new { role = model.VaiTro });
        }

        // ---------- Sửa ----------
        public async Task<IActionResult> Edit(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            return View("Form", new UserFormViewModel
            {
                Id = u.Id,
                HoTen = u.HoTen,
                Email = u.Email,
                SoDienThoai = u.SoDienThoai,
                VaiTro = u.VaiTro
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserFormViewModel model)
        {
            var u = await _db.Users.FindAsync(model.Id);
            if (u == null) return NotFound();

            var email = (model.Email ?? "").Trim().ToLower();

            if (await _db.Users.AnyAsync(x => x.Email == email && x.Id != model.Id))
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng");
            if (!AllRoles.Contains(model.VaiTro))
                ModelState.AddModelError(nameof(model.VaiTro), "Vai trò không hợp lệ");

            // Hạ quyền một Admin: không được tự hạ mình, và phải còn ít nhất một Admin hoạt động
            if (u.VaiTro == "Admin" && model.VaiTro != "Admin")
            {
                if (u.Id == CurrentUserId)
                    ModelState.AddModelError(nameof(model.VaiTro), "Bạn không thể tự hạ quyền của mình");
                else if (await IsLastActiveAdmin(u))
                    ModelState.AddModelError(nameof(model.VaiTro), "Phải còn ít nhất một quản trị viên đang hoạt động");
            }

            if (!ModelState.IsValid) return View("Form", model);

            u.HoTen = model.HoTen.Trim();
            u.Email = email;
            u.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();
            u.VaiTro = model.VaiTro;
            if (!string.IsNullOrEmpty(model.MatKhau))
                u.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau);

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã cập nhật tài khoản.";
            return RedirectToAction(nameof(Index), new { role = u.VaiTro });
        }

        // ---------- Khóa / mở khóa ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            if (u.TrangThai)   // chỉ kiểm tra khi định khóa, mở khóa thì luôn được
            {
                if (u.Id == CurrentUserId)
                {
                    TempData["Error"] = "Bạn không thể tự khóa chính mình.";
                    return RedirectToAction(nameof(Index), new { role = u.VaiTro });
                }
                if (await IsLastActiveAdmin(u))
                {
                    TempData["Error"] = "Phải còn ít nhất một quản trị viên đang hoạt động.";
                    return RedirectToAction(nameof(Index), new { role = u.VaiTro });
                }
            }

            u.TrangThai = !u.TrangThai;
            await _db.SaveChangesAsync();

            TempData["Msg"] = u.TrangThai ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
            return RedirectToAction(nameof(Index), new { role = u.VaiTro });
        }

        // ---------- Xóa ----------
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();

            var role = u.VaiTro;

            if (u.Id == CurrentUserId)
            {
                TempData["Error"] = "Bạn không thể tự xóa chính mình.";
                return RedirectToAction(nameof(Index), new { role });
            }
            if (await IsLastActiveAdmin(u))
            {
                TempData["Error"] = "Phải còn ít nhất một quản trị viên đang hoạt động.";
                return RedirectToAction(nameof(Index), new { role });
            }

            // Giữ lại lịch hẹn, chỉ gỡ liên kết tới tài khoản (UserId cho phép null)
            var appointments = await _db.Appointments.Where(a => a.UserId == id).ToListAsync();
            foreach (var a in appointments) a.UserId = null;

            _db.Users.Remove(u);   // Favorites của người này tự xóa theo (cascade)
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Đã xóa tài khoản.";
            return RedirectToAction(nameof(Index), new { role });
        }
    }
}