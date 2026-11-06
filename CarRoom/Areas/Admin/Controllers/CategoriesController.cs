using CarRoom.Data;
using CarRoom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _db;

        // Danh sách loại thân xe dùng chung cho form và bộ lọc ở trang khách
        public static readonly string[] BodyTypes = { "Sedan", "SUV", "Hatchback", "Coupe", "MPV", "Bán tải", "Xe điện" };

        public CategoriesController(AppDbContext db) => _db = db;

        // Trái: danh sách hãng. Phải: dòng xe của hãng đang chọn
        public async Task<IActionResult> Index(int? brandId, string? q)
        {
            var query = _db.Brands.AsNoTracking().Include(b => b.CarModels).AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(b => b.TenHang.Contains(q.Trim()));
            var brands = await query.OrderBy(b => b.TenHang).ToListAsync();

            var allBrands = await _db.Brands.AsNoTracking().OrderBy(b => b.TenHang).ToListAsync();
            int? selected = brandId ?? allBrands.FirstOrDefault()?.Id;

            ViewBag.AllBrands = allBrands;
            ViewBag.SelectedBrandId = selected;
            ViewBag.Q = q;
            ViewBag.Models = selected == null
                ? new List<CarModel>()
                : await _db.CarModels.AsNoTracking()
                    .Where(m => m.BrandId == selected)
                    .OrderBy(m => m.TenDongXe)
                    .ToListAsync();

            return View(brands);
        }

        // ---------------- HÃNG XE ----------------

        public IActionResult CreateBrand() => View("BrandForm", new Brand());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBrand([Bind("TenHang")] Brand model)
        {
            model.TenHang = (model.TenHang ?? "").Trim();
            if (await _db.Brands.AnyAsync(b => b.TenHang == model.TenHang))
                ModelState.AddModelError(nameof(model.TenHang), "Hãng này đã tồn tại");
            if (!ModelState.IsValid) return View("BrandForm", model);

            _db.Brands.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã thêm hãng xe.";
            return RedirectToAction(nameof(Index), new { brandId = model.Id });
        }

        public async Task<IActionResult> EditBrand(int id)
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View("BrandForm", brand);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBrand([Bind("Id,TenHang")] Brand model)
        {
            var brand = await _db.Brands.FindAsync(model.Id);
            if (brand == null) return NotFound();

            model.TenHang = (model.TenHang ?? "").Trim();
            if (await _db.Brands.AnyAsync(b => b.TenHang == model.TenHang && b.Id != model.Id))
                ModelState.AddModelError(nameof(model.TenHang), "Hãng này đã tồn tại");
            if (!ModelState.IsValid) return View("BrandForm", model);

            brand.TenHang = model.TenHang;
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã cập nhật hãng xe.";
            return RedirectToAction(nameof(Index), new { brandId = brand.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            // Chặn xóa khi còn dòng xe, tránh xóa dây chuyền mất cả xe, màu, lịch hẹn
            if (await _db.CarModels.AnyAsync(m => m.BrandId == id))
            {
                TempData["Error"] = "Hãng này còn dòng xe, hãy xóa hoặc chuyển các dòng xe trước.";
                return RedirectToAction(nameof(Index), new { brandId = id });
            }

            _db.Brands.Remove(brand);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã xóa hãng xe.";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- DÒNG XE ----------------

        public async Task<IActionResult> CreateModel(int? brandId)
        {
            await FillLists();
            return View("ModelForm", new CarModel { BrandId = brandId ?? 0, LoaiThanXe = BodyTypes[0] });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateModel([Bind("BrandId,TenDongXe,LoaiThanXe")] CarModel model)
        {
            await ValidateModel(model);
            if (!ModelState.IsValid)
            {
                await FillLists();
                return View("ModelForm", model);
            }

            _db.CarModels.Add(model);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã thêm dòng xe.";
            return RedirectToAction(nameof(Index), new { brandId = model.BrandId });
        }

        public async Task<IActionResult> EditModel(int id)
        {
            var m = await _db.CarModels.FindAsync(id);
            if (m == null) return NotFound();
            await FillLists();
            return View("ModelForm", m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditModel([Bind("Id,BrandId,TenDongXe,LoaiThanXe")] CarModel model)
        {
            var m = await _db.CarModels.FindAsync(model.Id);
            if (m == null) return NotFound();

            await ValidateModel(model);
            if (!ModelState.IsValid)
            {
                await FillLists();
                return View("ModelForm", model);
            }

            m.BrandId = model.BrandId;
            m.TenDongXe = model.TenDongXe;
            m.LoaiThanXe = model.LoaiThanXe;
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã cập nhật dòng xe.";
            return RedirectToAction(nameof(Index), new { brandId = m.BrandId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteModel(int id)
        {
            var m = await _db.CarModels.FindAsync(id);
            if (m == null) return NotFound();

            if (await _db.Cars.AnyAsync(c => c.CarModelId == id))
            {
                TempData["Error"] = "Dòng xe này còn xe trong kho, hãy xóa các xe đó trước.";
                return RedirectToAction(nameof(Index), new { brandId = m.BrandId });
            }

            var brandId = m.BrandId;
            _db.CarModels.Remove(m);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Đã xóa dòng xe.";
            return RedirectToAction(nameof(Index), new { brandId });
        }

        // ---------------- HÀM HỖ TRỢ ----------------

        private async Task FillLists()
        {
            ViewBag.Brands = await _db.Brands.AsNoTracking().OrderBy(b => b.TenHang).ToListAsync();
        }

        private async Task ValidateModel(CarModel m)
        {
            m.TenDongXe = (m.TenDongXe ?? "").Trim();

            if (!await _db.Brands.AnyAsync(b => b.Id == m.BrandId))
                ModelState.AddModelError(nameof(m.BrandId), "Vui lòng chọn hãng xe");
            if (!BodyTypes.Contains(m.LoaiThanXe))
                ModelState.AddModelError(nameof(m.LoaiThanXe), "Loại thân xe không hợp lệ");
            if (await _db.CarModels.AnyAsync(x => x.BrandId == m.BrandId && x.TenDongXe == m.TenDongXe && x.Id != m.Id))
                ModelState.AddModelError(nameof(m.TenDongXe), "Dòng xe này đã có trong hãng");
        }
    }
}