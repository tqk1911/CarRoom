
using CarRoom.Data;
using CarRoom.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CarRoom.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly AppDbContext _context;
        private const string SessionKey = "FavoriteCarIds";

        public FavoriteController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            List<int> carIds;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserId();

                if (userId == null)
                {
                    return Challenge();
                }

                carIds = await _context.Favorites
                    .AsNoTracking()
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.CarId)
                    .ToListAsync();
            }
            else
            {
                carIds = GetSessionFavorites();
            }

            var cars = await _context.Cars
                .Include(c => c.CarModel)
                    .ThenInclude(m => m.Brand)
                .AsNoTracking()
                .Where(c => carIds.Contains(c.Id))
                .ToListAsync();

            return View(cars);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int carId)
        {
            var carExists = await _context.Cars
                .AnyAsync(c => c.Id == carId);

            if (!carExists)
            {
                return NotFound();
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserId();

                if (userId == null)
                {
                    return Challenge();
                }

                var exists = await _context.Favorites
                    .AnyAsync(f => f.UserId == userId.Value && f.CarId == carId);

                if (!exists)
                {
                    var favorite = new Favorite
                    {
                        UserId = userId.Value,
                        CarId = carId,
                        NgayThem = DateTime.Now
                    };

                    _context.Favorites.Add(favorite);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var favorites = GetSessionFavorites();

                if (!favorites.Contains(carId))
                {
                    favorites.Add(carId);
                    SaveSessionFavorites(favorites);
                }
            }

            TempData["SuccessMessage"] = "Đã thêm xe vào danh sách yêu thích.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int carId)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetUserId();

                if (userId == null)
                {
                    return Challenge();
                }

                var favorite = await _context.Favorites
                    .FirstOrDefaultAsync(f =>
                        f.UserId == userId.Value && f.CarId == carId);

                if (favorite != null)
                {
                    _context.Favorites.Remove(favorite);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var favorites = GetSessionFavorites();

                favorites.Remove(carId);

                SaveSessionFavorites(favorites);
            }

            TempData["SuccessMessage"] = "Đã xóa xe khỏi danh sách yêu thích.";

            return RedirectToAction(nameof(Index));
        }

        private int? GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(claim, out int userId))
            {
                return userId;
            }

            return null;
        }

        private List<int> GetSessionFavorites()
        {
            var json = HttpContext.Session.GetString(SessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<int>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<int>>(json)
                    ?? new List<int>();
            }
            catch (JsonException)
            {
                return new List<int>();
            }
        }

        private void SaveSessionFavorites(List<int> favorites)
        {
            var json = JsonSerializer.Serialize(favorites);

            HttpContext.Session.SetString(SessionKey, json);
        }
    }
}
