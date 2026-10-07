using System.ComponentModel.DataAnnotations;

namespace CarRoom.Models.ViewModels
{
    public class AppointmentFormViewModel
    {
        [Required]
        public int CarId { get; set; }

        public string? TenXe { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string HoTenKhach { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn ngày và giờ hẹn.")]
        [Display(Name = "Ngày hẹn")]
        public DateTime NgayHen { get; set; } = DateTime.Now.AddDays(1);

        [StringLength(500, ErrorMessage = "Ghi chú không được dài quá 500 ký tự.")]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }
    }
}