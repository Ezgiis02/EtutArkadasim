using EtutArkadasim.Web.Models; // User için
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver; // IMongoDatabase için
using System.Diagnostics;
using System.Security.Claims; // ClaimTypes için

namespace EtutArkadasim.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<User> _usersCollection;

        public HomeController(IMongoDatabase database)
        {
            _database = database;
            _usersCollection = _database.GetCollection<User>("users");
        }

        // GÜNCELLENDÝ (Req 2): Ana Sayfa artýk dinamik
        public async Task<IActionResult> Index()
        {
            // 1. Kullanýcý giriþ yapmýþ mý?
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // 2. Evet, giriþ yapmýþ. Dashboard'u göster.
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized();
                }

                // 3. Kullanýcýyý veritabanýndan bul
                var user = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
                if (user == null)
                {
                    return NotFound("Kullanýcý bulunamadý.");
                }

                // 4. Kullanýcý modelini "Dashboard" View'ýna gönder
                return View("Dashboard", user);
            }

            // 5. Hayýr, giriþ yapmamýþ. Normal "Welcome" sayfasýný göster.
            return View("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
