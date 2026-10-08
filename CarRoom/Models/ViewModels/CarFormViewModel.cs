using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CarRoom.Models.ViewModels
{
    public class CarFormViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn dòng xe")]
        [Display(Name = "Dòng xe")]
        public int? CarModelId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập năm sản xuất")]
        [Range(1990, 2100, ErrorMessage = "Năm sản xuất phải từ 1990 đến 2100")]
        [Display(Name = "Năm sản xuất")]
        public int? NamSanXuat { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập giá")]
        [Range(1, 1000000000000, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá (VNĐ)")]
        public decimal? Gia {  get; set; }
        [Required(ErrorMessage = "Vui lòng nhập hộp số")]
        [StringLength(50, ErrorMessage = "Hộp số tối đa 50 ký tự")]
        [Display(Name = "Hộp số")]
        public string HopSo { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập nhiên liệu")]
        [StringLength(50, ErrorMessage = "Nhiên liệu tối đa 50 ký tự")]
        [Display(Name = "Nhiên liệu")]
        public string NhienLieu { get; set; } = "";
        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }
        [Display(Name = "Đang bán (hiển thị)")]
        public bool TrangThai { get; set; } = true;

        [Display(Name = "Xe nổi bật (hiện ở trang chủ)")]
        public bool NoiBat { get; set; }
        public List<CarColorInput> Colors { get; set; } = new();
        public List<SelectListItem> CarModels { get; set; } = new();

    }
    public class CarColorInput
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Nhập tên màu")]
        [StringLength(50, ErrorMessage = "Tên màu tối đa 50 kí tự")]
        public string TenMau { get; set; } = "";
        [Required(ErrorMessage = "Chọn mã màu")]
        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Mã màu dạng #RRGGBB")]
        public string MaMauHex { get; set; } = "#000000";
        [Range(0, 10000, ErrorMessage = "Tồn kho từ 0 đến 10000")]
        public int SoLuongTon { get; set; }
        public List<IFormFile> NewImages { get; set; } = new();
        public List<ExistingImage> ExistingImages { get; set; } = new();
        public List<int> RemoveImageIds { get; set; } = new();
    }
    public class ExistingImage
    {
        public int Id { get; set; }
        public string Url { get; set; } = "";
    }
}
