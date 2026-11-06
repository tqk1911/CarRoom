using System.ComponentModel.DataAnnotations;

namespace CarRoom.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string HoTen { get; set; } = "";
        public string Email { get; set; } = "";
        public string? SoDienThoai { get; set; }
        public List<Appointment> Appointments { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
    }

    public class UpdateProfileInput
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string HoTen { get; set; } = "";

        [RegularExpression(@"^(0|\+84)\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }
    }
}