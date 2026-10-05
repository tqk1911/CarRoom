using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
namespace CarRoom.Models
{
    public class Car
    {
        public int Id { get; set; }
        [Required]
        public int CarModelId { get; set; }
        [ForeignKey(nameof(CarModelId))]
        public CarModel? CarModel { get; set; }
        [Range(2000,2026, ErrorMessage ="Năm sản xuất không hợp lệ!")]
        public int NamSanXuat { get; set; }
        [Range(1,double.MaxValue, ErrorMessage ="Giá không hợp lệ!")]
        public decimal Gia { get; set; }
        public string HopSo { get; set; } = "Tự động";
        public string NhienLieu { get; set; } = "Xăng";
        public string? MoTa { get; set; }
        public string TrangThai { get; set; } = "Còn hàng";
        public string NoiBat { get; set; }
        public DateTime NgayDang { get; set; } = DateTime.Now;
        public List<CarColor> Colors { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
        [NotMapped]
        public bool ConHang => Colors.Any(c => c.SoLuongTon > 0) && TrangThai == "Còn hàng";
    }
}
