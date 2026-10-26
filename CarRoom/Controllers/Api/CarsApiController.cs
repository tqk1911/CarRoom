using System.Text.Json;
using CarRoom.Data;
using CarRoom.Models;
using CarRoom.Models.ViewModels;
using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRoom.Controllers.Api
{
    [ApiController]
    [Route("api/cars")]
    public class CarsApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly HtmlSanitizer _sanitizer = new();

        public CarsApiController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] CarFilterQuery f)
        {
            f.Page = Math.Max(1, f.Page);
            f.PageSize = Math.Clamp(f.PageSize, 1, 50);

            var query = _db.Cars.AsNoTracking().Where(c => c.TrangThai != "Ngừng bán");

            // Tick nhiều hãng / loại thân xe / nhiên liệu = lấy xe thuộc BẤT KỲ giá trị nào được tick
            if (f.BrandIds.Count > 0) query = query.Where(c => f.BrandIds.Contains(c.CarModel.BrandId));
            if (f.BodyTypes.Count > 0) query = query.Where(c => f.BodyTypes.Contains(c.CarModel.LoaiThanXe));
            if (f.Fuels.Count > 0) query = query.Where(c => f.Fuels.Contains(c.NhienLieu));

            if (f.MinPrice.HasValue) query = query.Where(c => c.Gia >= f.MinPrice.Value);
            if (f.MaxPrice.HasValue) query = query.Where(c => c.Gia <= f.MaxPrice.Value);
            if (f.Year.HasValue) query = query.Where(c => c.NamSanXuat == f.Year.Value);

            // Hai kiểu sắp xếp độc lập: nếu chọn giá thì sắp theo giá trước, cùng giá thì xếp theo năm sản xuất.
            // Không chọn giá thì chỉ sắp theo năm sản xuất (mặc định năm mới nhất).
            // Cùng năm thì xe đăng gần đây hơn xếp trước.
            var oldestYearFirst = f.YearSort == "oldest";
            IOrderedQueryable<Car> ordered;
            if (f.PriceSort == "asc" || f.PriceSort == "desc")
            {
                ordered = f.PriceSort == "asc" ? query.OrderBy(c => c.Gia) : query.OrderByDescending(c => c.Gia);
                ordered = oldestYearFirst ? ordered.ThenBy(c => c.NamSanXuat) : ordered.ThenByDescending(c => c.NamSanXuat);
            }
            else
            {
                ordered = oldestYearFirst ? query.OrderBy(c => c.NamSanXuat) : query.OrderByDescending(c => c.NamSanXuat);
            }
            query = ordered.ThenByDescending(c => c.NgayDang).ThenByDescending(c => c.Id);

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)f.PageSize));
            if (f.Page > totalPages) f.Page = totalPages;

            var items = await query
                .Skip((f.Page - 1) * f.PageSize)
                .Take(f.PageSize)
                .Select(c => new
                {
                    c.Id,
                    Name = c.CarModel.Brand.TenHang + " " + c.CarModel.TenDongXe,
                    Brand = c.CarModel.Brand.TenHang,
                    BodyType = c.CarModel.LoaiThanXe,
                    Year = c.NamSanXuat,
                    Price = c.Gia,
                    Fuel = c.NhienLieu,
                    Gearbox = c.HopSo,
                    InStock = c.Colors.Any(col => col.SoLuongTon > 0),
                    Image = c.Colors.SelectMany(col => col.CarImages)
                        .OrderByDescending(i => i.LaAnhChinh).ThenBy(i => i.ThuTu)
                        .Select(i => i.DuongDanAnh).FirstOrDefault(),
                    Colors = c.Colors.Select(col => col.MaMauHex).ToList()
                })
                .ToListAsync();

            SaveFilterCookie(f);

            return Ok(new { items, page = f.Page, pageSize = f.PageSize, totalItems, totalPages });
        }

        // GET /api/cars/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var car = await _db.Cars.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id, c.CarModelId, Name = c.CarModel.Brand.TenHang + " " + c.CarModel.TenDongXe, c.NamSanXuat,
                    c.Gia, c.HopSo, c.NhienLieu, c.MoTa, c.TrangThai, c.NoiBat, c.NgayDang, InStock = c.Colors.Any(col => col.SoLuongTon > 0),
                    Colors = c.Colors.Select(col => new
                    {
                        col.Id,
                        col.TenMau,
                        col.MaMauHex,
                        col.SoLuongTon,
                        Images = col.CarImages.OrderBy(i => i.ThuTu)
                            .Select(i => new { i.Id, i.DuongDanAnh, i.LaAnhChinh }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return car == null ? NotFound(new { message = "Không tìm thấy xe" }) : Ok(car);
        }

        // POST /api/cars   (Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CarApiDto dto)
        {
            if (!await _db.CarModels.AnyAsync(m => m.Id == dto.CarModelId))
                ModelState.AddModelError(nameof(dto.CarModelId), "Dòng xe không tồn tại");
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var car = new Car { NgayDang = DateTime.Now };
            Apply(car, dto);
            foreach (var c in dto.Colors)
                car.Colors.Add(new CarColor { TenMau = c.TenMau, MaMauHex = c.MaMauHex, SoLuongTon = c.SoLuongTon });

            _db.Cars.Add(car);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = car.Id }, new { car.Id });
        }

        // PUT /api/cars/5   (Admin)
        // Cập nhật thông tin xe. Màu có Id thì sửa, Id = 0 thì thêm mới; màu không gửi lên giữ nguyên (không xóa).
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CarApiDto dto)
        {
            var car = await _db.Cars.Include(c => c.Colors).FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound(new { message = "Không tìm thấy xe" });

            if (!await _db.CarModels.AnyAsync(m => m.Id == dto.CarModelId))
                ModelState.AddModelError(nameof(dto.CarModelId), "Dòng xe không tồn tại");
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            Apply(car, dto);

            foreach (var input in dto.Colors)
            {
                if (input.Id > 0)
                {
                    var color = car.Colors.FirstOrDefault(c => c.Id == input.Id);
                    if (color == null)
                        return BadRequest(new { message = $"Màu Id {input.Id} không thuộc xe này" });
                    color.TenMau = input.TenMau;
                    color.MaMauHex = input.MaMauHex;
                    color.SoLuongTon = input.SoLuongTon;
                }
                else
                {
                    car.Colors.Add(new CarColor { TenMau = input.TenMau, MaMauHex = input.MaMauHex, SoLuongTon = input.SoLuongTon });
                }
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/cars/5   (Admin)
        // Lưu ý: không xóa file ảnh trên đĩa ở API; xóa xe có ảnh nên dùng trang Admin để dọn file.
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var car = await _db.Cars.FindAsync(id);
            if (car == null) return NotFound(new { message = "Không tìm thấy xe" });

            try
            {
                _db.Cars.Remove(car);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Không thể xóa: xe đang có lịch hẹn hoặc dữ liệu liên quan. Hãy chuyển sang trạng thái ngừng bán." });
            }

            return NoContent();
        }

        private void Apply(Car car, CarApiDto dto)
        {
            car.CarModelId = dto.CarModelId!.Value;
            car.NamSanXuat = dto.NamSanXuat!.Value;
            car.Gia = dto.Gia!.Value;
            car.HopSo = dto.HopSo;
            car.NhienLieu = dto.NhienLieu;
            car.MoTa = string.IsNullOrWhiteSpace(dto.MoTa) ? null : _sanitizer.Sanitize(dto.MoTa);
            car.TrangThai = dto.TrangThai ? "Còn hàng" : "Ngừng bán";
            car.NoiBat = dto.NoiBat ? "Có" : "Không";
        }

        // Cookie lưu bộ lọc xe gần nhất, hạn 7 ngày. Trang Cars/Index đọc lại để điền sẵn form.
        private void SaveFilterCookie(CarFilterQuery f)
        {
            var toSave = new CarFilterQuery
            {
                BrandIds = f.BrandIds,
                BodyTypes = f.BodyTypes,
                Fuels = f.Fuels,
                MinPrice = f.MinPrice,
                MaxPrice = f.MaxPrice,
                Year = f.Year,
                PriceSort = f.PriceSort,
                YearSort = f.YearSort
            };

            Response.Cookies.Append(CarsController.FilterCookieName, JsonSerializer.Serialize(toSave),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(7),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });
        }
    }
}
