
using System.ComponentModel.DataAnnotations;
namespace CarRoom.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập họ tên!")]
        [StringLength(100)]
        public string HoTen { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập email!")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ!")]
        public string Email { get; set; } = "";
        public string? MatKhau { get; set; }
        public string? GoogleId { get; set; }
        [RegularExpression(@"^(0|+84)\d{9}$", ErrorMessage ="Số điện thoại không hợp lệ!")]
        public string? SoDienThoai { get; set; }
        public string VaiTro { get; set; } = "Customer";
        public bool TrangThai { get; set; } = true;
        public DateTime NgayTao { get; set; } = DateTime.Now;
        public List<Appointment> Appointments { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();


    }
}
