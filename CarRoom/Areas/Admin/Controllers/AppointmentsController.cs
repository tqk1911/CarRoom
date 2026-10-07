using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRoom.Data;

namespace CarRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class AppointmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AppointmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Appointments
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.Appointments
                .Include(a => a.Car)
                    .ThenInclude(c => c.CarModel)
                .AsNoTracking()
                .OrderByDescending(a => a.NgayHen)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.TrangThai == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string kw = search.Trim().ToLower();
                query = query.Where(a => a.HoTenKhach.ToLower().Contains(kw) ||
                                         a.SoDienThoai.Contains(kw) ||
                                         (a.Car != null && a.Car.CarModel != null && a.Car.CarModel.TenDongXe.ToLower().Contains(kw)));
            }

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            var appointments = await query.ToListAsync();
            return View(appointments);
        }

        // POST: /Admin/Appointments/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.TrangThai = "Đã xác nhận";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xác nhận lịch hẹn #{id} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Appointments/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.TrangThai = "Đã hủy";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy lịch hẹn #{id}.";
            return RedirectToAction(nameof(Index));
        }
    }
}