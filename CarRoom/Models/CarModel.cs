using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRoom.Models
{
    public class CarModel
    {
        public int Id { get; set; }
        [Required]
        public int BrandId { get; set; }
        [ForeignKey(nameof(BrandId))]
        public Brand? Brand { get; set; }
        [Required(ErrorMessage ="Vui lòng nhập tên dòng xe!")]
        [StringLength(100)]
        public string TenDongXe { get; set; } = "";
        public string LoaiThanXe { get; set; } = "Sendan";
        public List<Car> Cars = new();

    }
}
