
using CarRoom.Data;
using CarRoom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CarModelsController : Controller
    {
        private readonly AppDbContext _context;

        public CarModelsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, int? brandId)
        {
            var query = _context.CarModels
                .Include(m => m.Brand)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();

                query = query.Where(m =>
                    m.TenDongXe.Contains(keyword) ||
                    m.LoaiThanXe.Contains(keyword) ||
                    (m.Brand != null && m.Brand.TenHang.Contains(keyword)));
            }

            if (brandId.HasValue && brandId.Value > 0)
            {
                query = query.Where(m => m.BrandId == brandId.Value);
            }

            var models = await query
                .OrderBy(m => m.Brand!.TenHang)
                .ThenBy(m => m.TenDongXe)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.BrandId = brandId;
            await LoadBrands();

            return View(models);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadBrands();
            return View(new CarModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BrandId,TenDongXe,LoaiThanXe")] CarModel model)
        {
            Normalize(model);

            await ValidateModel(model);

            if (!ModelState.IsValid)
            {
                await LoadBrands(model.BrandId);
                return View(model);
            }

            var carModel = new CarModel
            {
                BrandId = model.BrandId,
                TenDongXe = model.TenDongXe,
                LoaiThanXe = model.LoaiThanXe
            };

            _context.CarModels.Add(carModel);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm dòng xe thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _context.CarModels
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (model == null)
            {
                return NotFound();
            }

            await LoadBrands(model.BrandId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,BrandId,TenDongXe,LoaiThanXe")] CarModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            Normalize(model);

            await ValidateModel(model, id);

            if (!ModelState.IsValid)
            {
                await LoadBrands(model.BrandId);
                return View(model);
            }

            var existing = await _context.CarModels.FindAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.BrandId = model.BrandId;
            existing.TenDongXe = model.TenDongXe;
            existing.LoaiThanXe = model.LoaiThanXe;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật dòng xe thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể cập nhật dòng xe. Vui lòng kiểm tra dữ liệu liên quan.");

                await LoadBrands(model.BrandId);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await _context.CarModels.FindAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            bool hasCars = await _context.Cars
                .AnyAsync(c => c.CarModelId == id);

            if (hasCars)
            {
                TempData["Error"] =
                    "Không thể xóa dòng xe vì đang có xe thuộc dòng này.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.CarModels.Remove(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa dòng xe thành công.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa dòng xe vì có dữ liệu liên quan.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadBrands(int? selectedId = null)
        {
            var brands = await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.TenHang)
                .ToListAsync();

            ViewBag.Brands = new SelectList(
                brands,
                nameof(Brand.Id),
                nameof(Brand.TenHang),
                selectedId);
        }

        private static void Normalize(CarModel model)
        {
            model.TenDongXe = model.TenDongXe?.Trim() ?? "";
            model.LoaiThanXe = model.LoaiThanXe?.Trim() ?? "";
        }

        private async Task ValidateModel(CarModel model, int? excludeId = null)
        {
            if (model.BrandId <= 0 ||
                !await _context.Brands.AnyAsync(b => b.Id == model.BrandId))
            {
                ModelState.AddModelError(
                    nameof(CarModel.BrandId),
                    "Vui lòng chọn hãng xe hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(model.TenDongXe))
            {
                ModelState.AddModelError(
                    nameof(CarModel.TenDongXe),
                    "Vui lòng nhập tên dòng xe.");
            }

            if (model.TenDongXe.Length > 100)
            {
                ModelState.AddModelError(
                    nameof(CarModel.TenDongXe),
                    "Tên dòng xe không được vượt quá 100 ký tự.");
            }

            if (string.IsNullOrWhiteSpace(model.LoaiThanXe))
            {
                ModelState.AddModelError(
                    nameof(CarModel.LoaiThanXe),
                    "Vui lòng nhập loại thân xe.");
            }

            bool duplicate = await _context.CarModels.AnyAsync(m =>
                m.BrandId == model.BrandId &&
                m.TenDongXe == model.TenDongXe &&
                (!excludeId.HasValue || m.Id != excludeId.Value));

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(CarModel.TenDongXe),
                    "Dòng xe này đã tồn tại trong hãng đã chọn.");
            }
        }
    }
}
