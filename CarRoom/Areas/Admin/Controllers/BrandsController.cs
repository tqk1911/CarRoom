
using CarRoom.Data;
using CarRoom.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandsController : Controller
    {
        private readonly AppDbContext _context;

        public BrandsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Brands
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();
                query = query.Where(b => b.TenHang.Contains(keyword));
            }

            var brands = await query
                .OrderBy(b => b.TenHang)
                .ToListAsync();

            ViewBag.Search = search;

            return View(brands);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Brand());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand model)
        {
            ModelState.Remove(nameof(Brand.CarModels));

            model.TenHang = model.TenHang?.Trim() ?? "";
            model.Logo = string.IsNullOrWhiteSpace(model.Logo)
                ? null
                : model.Logo.Trim();

            if (string.IsNullOrWhiteSpace(model.TenHang))
            {
                ModelState.AddModelError(
                    nameof(Brand.TenHang),
                    "Vui lòng nhập tên hãng xe.");
            }

            if (await _context.Brands.AnyAsync(
                b => b.TenHang == model.TenHang))
            {
                ModelState.AddModelError(
                    nameof(Brand.TenHang),
                    "Hãng xe này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var brand = new Brand
            {
                TenHang = model.TenHang,
                Logo = model.Logo
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm hãng xe thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            ModelState.Remove(nameof(Brand.CarModels));

            model.TenHang = model.TenHang?.Trim() ?? "";
            model.Logo = string.IsNullOrWhiteSpace(model.Logo)
                ? null
                : model.Logo.Trim();

            if (string.IsNullOrWhiteSpace(model.TenHang))
            {
                ModelState.AddModelError(
                    nameof(Brand.TenHang),
                    "Vui lòng nhập tên hãng xe.");
            }

            if (await _context.Brands.AnyAsync(
                b => b.Id != id && b.TenHang == model.TenHang))
            {
                ModelState.AddModelError(
                    nameof(Brand.TenHang),
                    "Hãng xe này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var brand = await _context.Brands.FindAsync(id);

            if (brand == null)
            {
                return NotFound();
            }

            brand.TenHang = model.TenHang;
            brand.Logo = model.Logo;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật hãng xe thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            if (brand == null)
            {
                return NotFound();
            }

            bool hasModels = await _context.CarModels
                .AnyAsync(m => m.BrandId == id);

            if (hasModels)
            {
                TempData["Error"] =
                    "Không thể xóa hãng xe vì đang có dòng xe thuộc hãng này.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa hãng xe thành công.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] =
                    "Không thể xóa hãng xe vì có dữ liệu liên quan.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
