using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using System.Security.Claims;

namespace CarRoom.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /Appointment/Book
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(AppointmentFormViewModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = "Dữ liệu nhập vào chưa hợp lệ.", errors });
                }

                TempData["ErrorMessage"] = "Thông tin đặt lịch chưa hợp lệ. Vui lòng kiểm tra lại.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            // Xử lý UserId: nếu đăng nhập thì lấy ID tài khoản, nếu vãng lai lấy ID đầu tiên trong DB để thỏa mãn NOT NULL
            int defaultUserId = 1;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out int parsedId))
                {
                    defaultUserId = parsedId;
                }
            }
            else
            {
                var firstUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync();
                if (firstUser != null)
                {
                    defaultUserId = firstUser.Id;
                }
            }

            var appointment = new Appointment
            {
                CarId = model.CarId,
                UserId = defaultUserId,
                HoTenKhach = model.HoTenKhach.Trim(),
                SoDienThoai = model.SoDienThoai.Trim(),
                NgayHen = model.NgayHen,
                TrangThai = "Đang chờ",
                GhiChu = model.GhiChu?.Trim(),
                NgayTao = DateTime.Now
            };

            await _context.Appointments.AddAsync(appointment);
            await _context.SaveChangesAsync();

            if (isAjax)
            {
                return Ok(new { success = true, message = "Đặt lịch hẹn xem xe thành công!" });
            }

            TempData["SuccessMessage"] = "Đặt lịch hẹn thành công! Showroom sẽ liên hệ lại với bạn sớm nhất.";
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }
    }
}