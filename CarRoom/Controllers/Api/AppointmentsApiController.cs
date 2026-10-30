
using CarRoom.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    }
}
