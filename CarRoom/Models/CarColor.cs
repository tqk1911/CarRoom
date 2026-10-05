using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CarRoom.Models
{
    public class CarColor
    {
        public int Id { get; set; }
        [Required]
        public int CarId { get; set; }
        [ForeignKey(nameof(CarId))]
        public Car? Car { get; set; }
        [Required(ErrorMessage ="Vui lòng nhập màu!")]
        public string TenMau { get; set; } = "";
        public string MaMauHex { get; set; } = "#000000";
        [Range(0, int.MaxValue, ErrorMessage ="Số lượng không hợp lệ!")]
        public int SoLuongTon { get; set; } = 0;
        public List<CarImage> CarImages { get; set; } = new();
        [NotMapped]
        public bool ConHang => SoLuongTon > 0;
    }
}
