using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 
namespace CarRoom.Models
{
    public class CarImage
    {
        public int Id { get; set; }
        [Required]
        public int CarColorId { get; set; }
        [ForeignKey(nameof(CarColorId))]
        [Required]
        public string DuongDanAnh { get; set; } = "";
        public bool LaAnhChinh { get; set; } 
        public int ThuTu { get; set; }
    }
}
