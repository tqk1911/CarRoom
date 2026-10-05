using System.ComponentModel.DataAnnotations;
namespace CarRoom.Models
{
    public class Brand
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên hãng xe!")]
        [StringLength(50)]
        public string TenHang { get; set; } = "";
        public string? Logo { get; set; }
        public List<CarModel> CarModels { get; set; } = new();
    }
}
