using EtutArkadasim.Web.Models;
using EtutArkadasim.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

[Route("api/rating")]
[ApiController]
[Authorize] // Sadece giriş yapanlar puan verebilir
public class RatingController : ControllerBase
{
    private readonly IMongoCollection<User> _usersCollection;

    public RatingController(IMongoDatabase database)
    {
        _usersCollection = database.GetCollection<User>("users");
    }

    // --- API ENDPOINT 9: BİR ARKADAŞA PUAN VER ---
    [HttpPost("rate")] // Rota: /api/rating/rate
    public async Task<IActionResult> RateFriend([FromBody] RatingViewModel viewModel)
    {
        // 1. Puanı veren (giriş yapmış) kullanıcıyı bul
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // 2. Model geçerli mi? (Score 1-5 arası mı?)
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // 3. Kullanıcı kendini puanlayamaz
        if (currentUserId == viewModel.RatedUserId)
        {
            return BadRequest(new { message = "Kullanıcı kendini puanlayamaz." });
        }

        // 4. Puanlanacak kullanıcı (RatedUser) var mı?
        var ratedUser = await _usersCollection.Find(u => u.Id == viewModel.RatedUserId).FirstOrDefaultAsync();
        if (ratedUser == null)
        {
            return NotFound(new { message = "Puanlanacak kullanıcı bulunamadı." });
        }

        // 5. GÜVENLİK KONTROLÜ: Puanı veren, puanlanan kişiyle "arkadaş" mı?
        // (Yani, puanlanan kişi, puan verenin favori listesinde mi?)
        var currentUser = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
        if (currentUser == null || !currentUser.FavoriteUserIds.Contains(viewModel.RatedUserId))
        {
            return Unauthorized(new { message = "Sadece arkadaş olduğunuz kullanıcıları puanlayabilirsiniz." });
        }

        // 6. PUAN VERME İŞLEMİ:
        // Puanlanan kullanıcının (ratedUser) 'ratings' listesine yeni puanı (Score) ekle
        var filter = Builders<User>.Filter.Eq(u => u.Id, viewModel.RatedUserId);
        var update = Builders<User>.Update.Push(u => u.Ratings, viewModel.Score);

        await _usersCollection.UpdateOneAsync(filter, update);

        // 7. Puanlanan kullanıcının güncel ortalamasını al
        // (Not: Bu, veritabanından tekrar okuma yapar, anlık veri için gereklidir)
        var updatedUser = await _usersCollection.Find(filter).FirstOrDefaultAsync();
        var newAverage = updatedUser?.AverageRating ?? 0;

        return Ok(new
        {
            message = $"{ratedUser.Name} başarıyla puanlandı.",
            newAverageRating = newAverage
        });
    }
}