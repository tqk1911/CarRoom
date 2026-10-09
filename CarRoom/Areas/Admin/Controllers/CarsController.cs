using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace CarRoom.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CarsController : Controller
    {
        private const string UploadFolder = "uploads/cars";
        private const long MaxImageBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".jfif" };
        private const int PageSize = 10;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly HtmlSanitizer _sanitizer = new();

        public CarsController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Cars.AsNoTracking().Include(c => c.CarModel).ThenInclude(m => m.Brand)
                .Include(c => c.Colors).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.CarModel.TenDongXe.Contains(s) || c.CarModel.Brand.TenHang.Contains(s));
            }
            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);
            var cars = await query.OrderByDescending(c => c.Id).Skip((page-1)*PageSize).Take(PageSize).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total = total;
            return View(cars);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new CarFormViewModel
            {
                NamSanXuat = DateTime.Now.Year,
                Colors = new List<CarColorInput> { new() { TenMau = "", MaMauHex = "#ffffff" } }
            };
            await LoadCarModels(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CarFormViewModel vm)
        {
            ValidateColorsAndImages(vm);
            if (!ModelState.IsValid)
            {
                await LoadCarModels(vm);
                return View(vm);
            }

            var car = new Car { NgayDang = DateTime.Now };
            ApplyScalars(car, vm);
            foreach(var input in vm.Colors)
            {
                var color = new CarColor { TenMau = input.TenMau, MaMauHex = input.MaMauHex, SoLuongTon = input.SoLuongTon };
                await AddNewImagesAsync(color, input);
                EnsureMainImage(color);
                car.Colors.Add(color);
            }

            _db.Cars.Add(car);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã thêm xe mới.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var car = await _db.Cars.AsNoTracking().Include(c => c.Colors).ThenInclude(col => col.CarImages)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (car == null)
                return NotFound();
            var vm = new CarFormViewModel
            {
                Id = car.Id,
                CarModelId = car.CarModelId,
                NamSanXuat = car.NamSanXuat,
                Gia = car.Gia,
                HopSo = car.HopSo,
                NhienLieu = car.NhienLieu,
                MoTa = car.MoTa,
                TrangThai = car.TrangThai,
                NoiBat = car.NoiBat,
                Colors = car.Colors.Select(col => new CarColorInput
                {
                    Id = col.Id,
                    TenMau = col.TenMau,
                    MaMauHex = col.MaMauHex,
                    SoLuongTon = col.SoLuongTon,
                    ExistingImages = col.CarImages.OrderBy(i => i.ThuTu).Select(i => new ExistingImage { Id = i.Id, Url = i.DuongDanAnh }).ToList()
                }).ToList()
            };
            await LoadCarModels(vm);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CarFormViewModel vm)
        {
            if (id != vm.Id)
                return BadRequest();
            var car = await _db.Cars.Include(c => c.Colors).ThenInclude(col => col.CarImages).FirstOrDefaultAsync(c => c.Id == id);
            if(car == null)
                return NotFound();
            ValidateColorsAndImages(vm);
            foreach (var input in vm.Colors.Where(c => c.Id > 0))
                if (car.Colors.All(c => c.Id != input.Id))
                    return BadRequest();
            if (!ModelState.IsValid)
            {
                RefillExistingImages(vm, car);
                await LoadCarModels(vm);
                return View(vm);
            }
            ApplyScalars(car, vm);
            var filesToDelete = new List<string>();

            var postedIds = vm.Colors.Where(c => c.Id > 0).Select(c => c.Id).ToHashSet();
            foreach (var removed in car.Colors.Where(c => !postedIds.Contains(c.Id)).ToList())
            {
                filesToDelete.AddRange(removed.CarImages.Select(i => i.DuongDanAnh));
                _db.CarImages.RemoveRange(removed.CarImages);
                _db.CarColors.Remove(removed);
            }

            foreach(var input in vm.Colors)
            {
                CarColor color;
                if(input.Id > 0)
                {
                    color = car.Colors.First(c => c.Id == input.Id);
                }
                else
                {
                    color = new CarColor();
                    car.Colors.Add(color);
                }
                color.TenMau = input.TenMau;
                color.MaMauHex = input.MaMauHex;
                color.SoLuongTon = input.SoLuongTon;

                foreach(var img in color.CarImages.Where(i => input.RemoveImageIds.Contains(i.Id)).ToList())
                {
                    filesToDelete.Add(img.DuongDanAnh);
                    color.CarImages.Remove(img);
                    _db.CarImages.Remove(img);
                }
                await AddNewImagesAsync(color, input);
                EnsureMainImage(color);
            }
            await _db.SaveChangesAsync();
            foreach (var path in filesToDelete)
                DeleteImageFile(path);
            TempData["Success"] = "Đã cập nhật xe.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var car = await _db.Cars.Include(c => c.Colors).ThenInclude(col => col.CarImages).FirstOrDefaultAsync(c => c.Id == id);
            if(car == null)
                return NotFound();
            var files = car.Colors.SelectMany(c => c.CarImages).Select(i => i.DuongDanAnh).ToList();
            try
            {
                _db.CarImages.RemoveRange(car.Colors.SelectMany(c => c.CarImages));
                _db.CarColors.RemoveRange(car.Colors);
                _db.Cars.Remove(car);
                await _db.SaveChangesAsync();
            }
            catch(DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa xe này vì đã có lịch hẹn hoặc dữ liệu liên quan. Hãy bỏ chọn \"Đang bán\" để ẩn xe.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var path in files)
                DeleteImageFile(path);
            TempData["Success"] = "Đã xóa xe.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Sell(int colorId, string? returnSearch, int returnPage = 1)
        {
            var affected = await _db.CarColors.Where(c => c.Id == colorId && c.SoLuongTon > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.SoLuongTon, c => c.SoLuongTon - 1));
            if (affected == 1)
                TempData["Success"] = "Đã bán 1 chiếc, tồn kho của màu này giảm 1.";
            else
                TempData["Error"] = "Màu này đã hết hàng hoặc không tồn tại.";
            return RedirectToAction(nameof(Index), new { search = returnSearch, page = returnPage });
        }
        private async Task LoadCarModels(CarFormViewModel vm)
        {
            var models = await _db.CarModels.AsNoTracking().Include(m => m.Brand)
                .OrderBy(m => m.Brand.TenHang).ThenBy(m => m.TenDongXe).ToListAsync();
            var groups = new Dictionary<string, SelectListGroup>();
            vm.CarModels = models.Select(m =>
            {
                if (!groups.TryGetValue(m.Brand.TenHang, out var g))
                    groups[m.Brand.TenHang] = g = new SelectListGroup { Name = m.Brand.TenHang };
                return new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = $"{m.TenDongXe} ({m.LoaiThanXe})",
                    Group = g
                };
            }).ToList();
        }
        private void ApplyScalars(Car car, CarFormViewModel vm)
        {
            car.CarModelId = vm.CarModelId!.Value;
            car.NamSanXuat = vm.NamSanXuat!.Value;
            car.Gia = vm.Gia!.Value;
            car.HopSo = vm.HopSo.Trim();
            car.NhienLieu = vm.NhienLieu.Trim();
            car.MoTa = string.IsNullOrWhiteSpace(vm.MoTa) ? null : _sanitizer.Sanitize(vm.MoTa);
            car.TrangThai = vm.TrangThai;
            car.NoiBat = vm.NoiBat;
        }
        private void ValidateColorsAndImages(CarFormViewModel vm)
        {
            if (vm.Colors.Count == 0)
                ModelState.AddModelError("", "Xe phải có ít nhất 1 màu.");
            var dupNames = vm.Colors.GroupBy(c => c.TenMau.Trim().ToLowerInvariant())
                .Where(g => g.Key != "" && g.Count() > 1).Select(g => g.Key);
            foreach (var name in dupNames)
                ModelState.AddModelError("", $"Tên màu \"{name}\" bị trùng.");
            for(int i=0; i < vm.Colors.Count; i++)
            {
                foreach(var file in vm.Colors[i].NewImages)
                {
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if(!AllowedExtensions.Contains(ext) || !file.ContentType.StartsWith("image/"))
                        ModelState.AddModelError($"Colors[{i}].NewImages", $"\"{file.FileName}\": chỉ nhận ảnh jpg, png, webp, jfif.");
                    else if(file.Length > MaxImageBytes)
                        ModelState.AddModelError($"Colors[{i}].NewImages", $"\"{file.FileName}\" vượt quá 5MB.");
                }
            }
        }
        private async Task AddNewImagesAsync(CarColor color, CarColorInput input)
        {
            var folder = Path.Combine(_env.WebRootPath, UploadFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folder);
            var order = color.CarImages.Any() ? color.CarImages.Max(i => i.ThuTu) + 1 : 1;
            foreach(var file in input.NewImages.Where(f => f.Length > 0))
            {
                var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
                await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    await file.CopyToAsync(stream);
                color.CarImages.Add(new CarImage
                {
                    DuongDanAnh = $"/{UploadFolder}/{fileName}",
                    ThuTu = order++,
                    LaAnhChinh = false
                });
            }
        }
        private static void EnsureMainImage(CarColor color)
        {
            if (!color.CarImages.Any())
                return;
            if (color.CarImages.Count(i => i.LaAnhChinh) == 1)
                return;
            foreach (var img in color.CarImages)
                img.LaAnhChinh = false;
            color.CarImages.OrderBy(i => i.ThuTu).First().LaAnhChinh = true;
        }
        private void DeleteImageFile(string urlPath)
        {
            if (string.IsNullOrWhiteSpace(urlPath))
                return;
            var relative = urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, relative));
            var allowedRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, UploadFolder.Replace('/', Path.DirectorySeparatorChar)));
            if(full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(full))
            {
                try
                {
                    System.IO.File.Delete(full);
                }
                catch (IOException)
                {
                    
                }
            }
        }
        private static void RefillExistingImages(CarFormViewModel vm, Car car)
        {
            foreach(var input in vm.Colors.Where(c => c.Id > 0))
            {
                var color = car.Colors.FirstOrDefault(c => c.Id == input.Id);
                if (color == null)
                    continue;
                input.ExistingImages = color.CarImages.OrderBy(i => i.ThuTu)
                    .Select(i => new ExistingImage { Id = i.Id, Url = i.DuongDanAnh}).ToList();
            }
        }
    }
}
