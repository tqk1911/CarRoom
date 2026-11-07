
using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRoom.Controllers.Api
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize(Roles = "Admin,Staff")]
    public class AppointmentsApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AppointmentsApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var appointments = await _context.Appointments
                .AsNoTracking()
                .OrderByDescending(a => a.NgayHen)
                .Select(a => new
                {
                    a.Id,
                    a.CarId,
                    a.UserId,
                    a.HoTenKhach,
                    a.SoDienThoai,
                    a.NgayHen,
                    a.TrangThai,
                    a.GhiChu,
                    a.NgayTao
                })
                .ToListAsync();

            return Ok(appointments);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.Id,
                    a.CarId,
                    a.UserId,
                    a.HoTenKhach,
                    a.SoDienThoai,
                    a.NgayHen,
                    a.TrangThai,
                    a.GhiChu,
                    a.NgayTao
                })
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy lịch hẹn."
                });
            }

            return Ok(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] AppointmentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (model.NgayHen <= DateTime.Now)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Ngày giờ hẹn phải lớn hơn thời gian hiện tại."
                });
            }

            var carExists = await _context.Cars
                .AnyAsync(c => c.Id == model.CarId);

            if (!carExists)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Xe bạn chọn không tồn tại."
                });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Không xác định được tài khoản."
                });
            }

            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId && u.TrangThai);

            if (!userExists)
            {
                return Forbid();
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

            return CreatedAtAction(
                nameof(GetById),
                new { id = appointment.Id },
                new
                {
                    success = true,
                    message = "Tạo lịch hẹn thành công.",
                    appointment.Id,
                    appointment.CarId,
                    appointment.UserId,
                    appointment.HoTenKhach,
                    appointment.SoDienThoai,
                    appointment.NgayHen,
                    appointment.TrangThai,
                    appointment.GhiChu,
                    appointment.NgayTao
                });
        }

        [HttpPut("{id:int}/confirm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy lịch hẹn."
                });
            }

            if (appointment.TrangThai != "Đang chờ")
            {
                return BadRequest(new
                {
                    message = "Chỉ có thể xác nhận lịch hẹn đang chờ."
                });
            }

            appointment.TrangThai = "Đã xác nhận";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xác nhận lịch hẹn thành công.",
                appointment.Id,
                appointment.TrangThai
            });
        }

        [HttpPut("{id:int}/cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy lịch hẹn."
                });
            }

            if (appointment.TrangThai == "Đã hủy")
            {
                return BadRequest(new
                {
                    message = "Lịch hẹn này đã được hủy."
                });
            }

            appointment.TrangThai = "Đã hủy";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Hủy lịch hẹn thành công.",
                appointment.Id,
                appointment.TrangThai
            });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy lịch hẹn."
                });
            }

            if (appointment.TrangThai != "Đã hủy")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Chỉ được xóa lịch hẹn đã hủy."
                });
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xóa lịch hẹn thành công.",
                id
            });
        }
    }
}
