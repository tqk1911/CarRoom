
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(AppointmentFormViewModel model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            IActionResult ErrorResponse(string message)
            {
                if (isAjax)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message
                    });
                }

                TempData["ErrorMessage"] = message;
                return RedirectToAction("Details", "Cars", new { id = model.CarId });
            }

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu nhập vào chưa hợp lệ.",
                        errors
                    });
                }

                return ErrorResponse("Thông tin đặt lịch chưa hợp lệ. Vui lòng kiểm tra lại.");
            }

            if (model.NgayHen <= DateTime.Now)
            {
                return ErrorResponse("Ngày giờ hẹn phải lớn hơn thời gian hiện tại.");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Challenge();
            }

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId && u.TrangThai);

            if (!userExists)
            {
                return Forbid();
            }

            var carExists = await _context.Cars
                .AnyAsync(c => c.Id == model.CarId);

            if (!carExists)
            {
                return ErrorResponse("Xe bạn chọn không tồn tại.");
            }

            var appointment = new Appointment
            {
                CarId = model.CarId,
                UserId = userId,
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
                return Ok(new
                {
                    success = true,
                    message = "Đặt lịch hẹn xem xe thành công!"
                });
            }

            TempData["SuccessMessage"] =
                "Đặt lịch hẹn thành công! Showroom sẽ liên hệ lại với bạn sớm nhất.";

            return RedirectToAction("Details", "Cars", new { id = model.CarId });
        }
    }
}
