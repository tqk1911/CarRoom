
using CarRoom.Data;
using CarRoom.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfNextMonth = startOfMonth.AddMonths(1);
            var startOf30Days = today.AddDays(-29);
            var startOf12Months = startOfMonth.AddMonths(-11);

            var model = new DashboardViewModel
            {
                TongSoXe = await _context.Cars.CountAsync(),
                TongNguoiDung = await _context.Users.CountAsync(),
                TongLichHen = await _context.Appointments.CountAsync(),

                LichHenThangNay = await _context.Appointments
                    .CountAsync(a => a.NgayTao >= startOfMonth &&
                                     a.NgayTao < startOfNextMonth),

                LichHenDangCho = await _context.Appointments
                    .CountAsync(a => a.TrangThai == "Đang chờ"),

                LichHenDaXacNhan = await _context.Appointments
                    .CountAsync(a => a.TrangThai == "Đã xác nhận"),

                LichHenDaHuy = await _context.Appointments
                    .CountAsync(a => a.TrangThai == "Đã hủy"),

                LichHenGanDay = await _context.Appointments
                    .AsNoTracking()
                    .OrderByDescending(a => a.NgayTao)
                    .Take(6)
                    .Select(a => new DashboardAppointmentItem
                    {
                        Id = a.Id,
                        HoTenKhach = a.HoTenKhach,
                        TenXe = a.Car != null && a.Car.CarModel != null
                            ? a.Car.CarModel.TenDongXe
                            : "Chưa có thông tin",
                        NgayHen = a.NgayHen,
                        TrangThai = a.TrangThai
                    })
                    .ToListAsync(),

                ThongKeDongXe = await _context.Appointments
                    .AsNoTracking()
                    .Where(a => a.Car != null &&
                                a.Car.CarModel != null)
                    .GroupBy(a => a.Car!.CarModel!.TenDongXe)
                    .Select(g => new DashboardCarStatistic
                    {
                        TenDongXe = g.Key,
                        SoLichHen = g.Count()
                    })
                    .OrderByDescending(x => x.SoLichHen)
                    .Take(6)
                    .ToListAsync()
            };

            var dailyCounts = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.NgayTao >= startOf30Days &&
                            a.NgayTao < today.AddDays(1))
                .GroupBy(a => a.NgayTao.Date)
                .Select(g => new
                {
                    Ngay = g.Key,
                    SoLichHen = g.Count()
                })
                .ToListAsync();

            model.ThongKe30Ngay = Enumerable.Range(0, 30)
                .Select(offset =>
                {
                    var date = startOf30Days.AddDays(offset);

                    return new DashboardTimeStatistic
                    {
                        Nhan = date.ToString("dd/MM"),
                        SoLichHen = dailyCounts
                            .Where(x => x.Ngay == date)
                            .Sum(x => x.SoLichHen)
                    };
                })
                .ToList();

            model.ThongKeTheoNgay = Enumerable
                .Range(1, DateTime.DaysInMonth(today.Year, today.Month))
                .Select(day =>
                {
                    var date = startOfMonth.AddDays(day - 1);

                    return new DashboardDailyStatistic
                    {
                        Ngay = day,
                        SoLichHen = dailyCounts
                            .Where(x => x.Ngay == date)
                            .Sum(x => x.SoLichHen)
                    };
                })
                .ToList();

            var monthlyCounts = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.NgayTao >= startOf12Months &&
                            a.NgayTao < startOfNextMonth)
                .GroupBy(a => new
                {
                    a.NgayTao.Year,
                    a.NgayTao.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    SoLichHen = g.Count()
                })
                .ToListAsync();

            model.ThongKe12Thang = Enumerable.Range(0, 12)
                .Select(offset =>
                {
                    var month = startOf12Months.AddMonths(offset);

                    return new DashboardTimeStatistic
                    {
                        Nhan = month.ToString("MM/yyyy"),
                        SoLichHen = monthlyCounts
                            .Where(x => x.Year == month.Year &&
                                        x.Month == month.Month)
                            .Sum(x => x.SoLichHen)
                    };
                })
                .ToList();

            return View(model);
        }
    }
}
