using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CarRoom.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        [Required]
        public int CarId { get; set; }
        [ForeignKey(nameof(CarId))]
        public Car? Car { get; set; }
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTenKhach { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SoDienThoai { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn ngày giờ hẹn")]
        public DateTime NgayHen { get; set; }

        public string TrangThai { get; set; } = "Đang chờ"; 
        public string? GhiChu { get; set; }
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
