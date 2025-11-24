using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace EtutArkadasim.Web.Controllers
{
    [Authorize] // Bu sayfayı sadece giriş yapmış kullanıcılar görebilir
    public class DiscoverController : Controller
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<User> _usersCollection;

        public DiscoverController(IMongoDatabase database) { 
        _database = database;
            _usersCollection = _database.GetCollection<User>("users");
        }

        // Burası bir API değil, "View" (HTML Sayfası) döndüren klasik bir MVC metodu
        // Rota: /Discover/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Giriş yapan kullanıcının kimliğini al
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Veritabanından "kendisi hariç" tüm kullanıcıları çek
            // Not: Gerçek bir uygulamada burada sayfalama (pagination) kullanılır,
            // ancak biz şimdilik basit tutuyoruz.
            var filter = Builders<User>.Filter.Ne(u => u.Id, currentUserId);
            var allOtherUsers = await _usersCollection.Find(filter).ToListAsync();

            // 3. Kullanıcı listesini (List<User>) View'a gönder
            return View(allOtherUsers);
        }
    }
}