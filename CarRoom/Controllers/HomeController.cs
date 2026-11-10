
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRoom.Data;

namespace CarRoom.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var newCars = await _context.Cars
                .Include(c => c.CarModel)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Colors)
                    .ThenInclude(col => col.CarImages)
                .Where(c => c.TrangThai != "Ngừng bán"
                         && c.Colors.Any(col => col.SoLuongTon > 0))
                .OrderByDescending(c => c.NgayDang)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            var featuredCars = await _context.Cars
                .Include(c => c.CarModel)
                    .ThenInclude(m => m.Brand)
                .Include(c => c.Colors)
                    .ThenInclude(col => col.CarImages)
                .Where(c => c.NoiBat == "Có"
                         && c.TrangThai != "Ngừng bán"
                         && c.Colors.Any(col => col.SoLuongTon > 0))
                .OrderByDescending(c => c.NgayDang)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.FeaturedCars = featuredCars;

            return View(newCars);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
