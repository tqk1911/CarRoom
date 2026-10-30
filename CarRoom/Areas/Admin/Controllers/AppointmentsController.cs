
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

        public async Task<IActionResult> Index(
            string? status,
            string? search,
            int page = 1)
        {
            const int pageSize = 10;

            if (page < 1)
            {
                page = 1;
            }

            var query = _context.Appointments
                .Include(a => a.Car)
                    .ThenInclude(c => c.CarModel)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.TrangThai == status);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                query = query.Where(a =>
                    a.HoTenKhach.ToLower().Contains(keyword) ||
                    a.SoDienThoai.Contains(keyword) ||
                    (a.Car != null &&
                     a.Car.CarModel != null &&
                     a.Car.CarModel.TenDongXe.ToLower().Contains(keyword)));
            }

            int totalItems = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var appointments = await query
                .OrderByDescending(a => a.NgayHen)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            return View(appointments);
        }

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

            TempData["SuccessMessage"] =
                $"Đã xác nhận lịch hẹn #{id} thành công.";

            return RedirectToAction(nameof(Index));
        }

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

            TempData["SuccessMessage"] =
                $"Đã hủy lịch hẹn #{id}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
