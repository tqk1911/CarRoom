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

        // GET: / (Trang chủ)
        public async Task<IActionResult> Index()
        {
            // 1. Lấy 8 xe mới nhất đang bán
            var newCars = await _context.Cars
                .Include(c => c.CarModel)
                    .ThenInclude(m => m.Brand)
                .Where(c => c.TrangThai == "Đang bán")
                .OrderByDescending(c => c.NgayDang)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            // 2. Lấy 1 xe nổi bật cho Hero Banner (xử lý an toàn thuộc tính NoiBat kiểu string)
            var featuredCar = await _context.Cars
                .Include(c => c.CarModel)
                    .ThenInclude(m => m.Brand)
                .Where(c => (c.NoiBat == "True" || c.NoiBat == "Co" || c.NoiBat == "1") && c.TrangThai == "Đang bán")
                .OrderByDescending(c => c.NgayDang)
                .FirstOrDefaultAsync();

            if (featuredCar == null)
            {
                featuredCar = newCars.FirstOrDefault();
            }

            ViewBag.FeaturedCar = featuredCar;

            return View(newCars);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}