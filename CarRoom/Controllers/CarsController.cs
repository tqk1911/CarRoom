using System.Text.Json;
using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Controllers
{
    public class CarsController : Controller
    {
        public const string FilterCookieName = "carFilter";
        private readonly AppDbContext _db;
        public CarsController(AppDbContext db) => _db = db;
        public async Task<IActionResult> Index()
        {
            var activeCars = _db.Cars.AsNoTracking().Where(c => c.TrangThai != "Ngừng bán");
            var vm = new CarIndexViewModel
            {
                Brands = await _db.Brands.AsNoTracking().OrderBy(b => b.TenHang)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.TenHang }).ToListAsync(),
                BodyTypes = await activeCars.Select(c => c.CarModel.LoaiThanXe).Distinct().OrderBy(x => x).ToListAsync(),
                Fuels = await activeCars.Select(c => c.NhienLieu).Distinct().OrderBy(x => x).ToListAsync(),
                Years = await activeCars.Select(c => c.NamSanXuat).Distinct().OrderByDescending(x => x).ToListAsync(),
                Filter = ReadFilterCookie()
            };
            return View(vm);
        }
        public async Task<IActionResult> Detail(int id)
        {
            var car = await _db.Cars.AsNoTracking().Include(c => c.CarModel).ThenInclude(m => m.Brand)
                .Include(c => c.Colors).ThenInclude(col => col.CarImages).FirstOrDefaultAsync(c => c.Id == id && c.TrangThai != "Ngừng bán");
            if (car == null)
                return NotFound();
            // Xe tương tự: cùng loại thân xe hoặc cùng hãng, tối đa 4 xe, ưu tiên cùng loại thân xe và giá gần nhau
            var bodyType = car.CarModel.LoaiThanXe;
            var brandId = car.CarModel.BrandId;
            var similar = await _db.Cars.AsNoTracking().Include(c => c.CarModel).ThenInclude(m => m.Brand)
                .Include(c => c.Colors).ThenInclude(col => col.CarImages)
                .Where(c => c.Id != car.Id && c.TrangThai != "Ngừng bán" &&
                    (c.CarModel.LoaiThanXe == bodyType || c.CarModel.BrandId == brandId))
                .OrderByDescending(c => c.CarModel.LoaiThanXe == bodyType)
                .ThenBy(c => Math.Abs(c.Gia - car.Gia)).Take(4).ToListAsync();
            return View(new CarDetailViewModel { Car = car, SimilarCars = similar });
        }
        private CarFilterQuery ReadFilterCookie()
        {
            if (!Request.Cookies.TryGetValue(FilterCookieName, out var json) || string.IsNullOrWhiteSpace(json))
                return new CarFilterQuery();

            try
            {
                var filter = JsonSerializer.Deserialize<CarFilterQuery>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (filter != null)
                    filter.Page = 1;
                return filter ?? new CarFilterQuery();
            }
            catch (JsonException)
            {
                return new CarFilterQuery();
            }
        }
    }
}
