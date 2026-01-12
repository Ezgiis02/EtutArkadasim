using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq; // LINQ eklemeyi unutma

namespace EtutArkadasim.Web.Controllers
{
    [Authorize]
    public class RateController : Controller
    {
        private readonly IMongoCollection<User> _usersCollection;

        public RateController(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateUser(string userId, int rating)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(userId) || rating < 1 || rating > 5) return BadRequest();
            if (currentUserId == userId) return RedirectToAction("Dashboard", "Home");

            // Hedef kullanıcıyı çek
            var targetUser = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (targetUser == null) return NotFound();

            // Daha önce puan vermiş miyim?
            var existingRating = targetUser.Ratings.FirstOrDefault(r => r.RaterUserId == currentUserId);

            if (existingRating != null)
            {
                // VARSA GÜNCELLE: Listede senin ID'ne sahip kaydı bul ve skorunu değiştir
                var filter = Builders<User>.Filter.And(
                    Builders<User>.Filter.Eq(u => u.Id, userId),
                    Builders<User>.Filter.ElemMatch(u => u.Ratings, r => r.RaterUserId == currentUserId)
                );

                // MongoDB'nin özel operatörü ($) ile eşleşen elemanı güncelliyoruz
                var update = Builders<User>.Update.Set("ratings.$.Score", rating);
                await _usersCollection.UpdateOneAsync(filter, update);
            }
            else
            {
                // YOKSA YENİ EKLE
                var newRating = new UserRating { RaterUserId = currentUserId, Score = rating };
                var update = Builders<User>.Update.Push(u => u.Ratings, newRating);
                await _usersCollection.UpdateOneAsync(u => u.Id == userId, update);
            }

            TempData["SuccessMessage"] = "Puanın kaydedildi.";
            return RedirectToAction("Dashboard", "Home");
        }
    }
}