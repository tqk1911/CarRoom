using System.ComponentModel.DataAnnotations.Schema;
namespace CarRoom.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public int CarId { get; set; }

        [ForeignKey(nameof(CarId))]
        public Car? Car { get; set; }

        public DateTime NgayThem { get; set; } = DateTime.Now;
    }
}
