using EtutArkadasim.Web.Models;
using EtutArkadasim.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace EtutArkadasim.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<User> _usersCollection;

        public AccountController(IMongoDatabase database)
        {
            _database = database;
            _usersCollection = _database.GetCollection<User>("users");
        }

        // --- KAYIT (REGISTER) METOTLARI ---
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _usersCollection.Find(u => u.Email == model.Email.ToLower()).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                    return View(model);
                }

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                var newUser = new User
                {
                    Name = model.Name,
                    Email = model.Email.ToLower(),
                    PasswordHash = hashedPassword,
                    ProfileImageUrl = "" // Başlangıçta boş
                };

                await _usersCollection.InsertOneAsync(newUser);

                TempData["SuccessMessage"] = "Kaydınız başarıyla oluşturuldu. Lütfen giriş yapınız.";
                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }

        // --- GİRİŞ (LOGIN) METOTLARI ---
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _usersCollection.Find(u => u.Email == model.Email.ToLower()).FirstOrDefaultAsync();

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(30))
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Giriş başarılı, Ana Sayfa'ya yönlendir (Artık Dashboard'u gösterecek)
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
                return View(model);
            }
            return View(model);
        }

        // --- ÇIKIŞ (LOGOUT) METODU ---
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // --- (Req 3 & 4): PROFİL SAYFASI ARTIK "PROFİLİ DÜZENLE" SAYFASI ---
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile() // _Layout.cshtml'deki link buraya geliyor
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            var user = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Veritabanı modelini ViewModel'e dönüştür
            var viewModel = new EditProfileViewModel
            {
                Name = user.Name,
                ProfileImageUrl = user.ProfileImageUrl
            };

            return View(viewModel); // Views/Account/Profile.cshtml'i döndür
        }

        // (Req 4): PROFİLİ GÜNCELLEME METODU
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Hata varsa formu tekrar göster
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized();
            }

            // Kullanıcının adını ve fotoğraf URL'sini güncelle
            var filter = Builders<User>.Filter.Eq(u => u.Id, currentUserId);
            var update = Builders<User>.Update
                .Set(u => u.Name, model.Name)
                .Set(u => u.ProfileImageUrl, model.ProfileImageUrl ?? string.Empty); // Null gelirse diye kontrol

            await _usersCollection.UpdateOneAsync(filter, update);

            // ÖNEMLİ: Kullanıcının Cookie'sini (Oturum) yeni adıyla güncellemeliyiz
            // 1. Mevcut kimliği al
            var principal = User;
            var identity = (ClaimsIdentity)principal.Identity;

            // 2. 'Name' claim'ini bul ve kaldır
            var nameClaim = identity.FindFirst(ClaimTypes.Name);
            if (nameClaim != null)
            {
                identity.RemoveClaim(nameClaim);
            }

            // 3. Yeni 'Name' claim'ini ekle
            identity.AddClaim(new Claim(ClaimTypes.Name, model.Name));

            // 4. Oturumu yeniden aç (cookie'yi güncelle)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Başarı mesajı ekle ve Dashboard'a yönlendir
            TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
            return RedirectToAction("Index", "Home"); // Dashboard'a yönlendir
        }
    }
}