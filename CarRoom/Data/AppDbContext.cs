using CarRoom.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
namespace CarRoom.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Car> Cars => Set<Car>();
        public DbSet<Favorite> Favorites => Set<Favorite>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<CarModel> CarModels => Set<CarModel>();
        public DbSet<CarImage> CarImages => Set<CarImage>();
        public DbSet<CarColor> CarColors => Set<CarColor>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<Car>()
                .Property(c => c.Gia)
                .HasPrecision(18, 0);
        }   



    }
}
