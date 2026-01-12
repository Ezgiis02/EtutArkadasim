using EtutArkadasim.Web.Models;
using EtutArkadasim.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace EtutArkadasim.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Department> _departmentsCollection;

        // YENİ: Sadece şehir listesi için
        private readonly IMongoCollection<CityData> _citiesCollection;

        private readonly IWebHostEnvironment _environment;

        public AccountController(IMongoDatabase database, IWebHostEnvironment environment)
        {
            _database = database;
            _usersCollection = _database.GetCollection<User>("users");
            _departmentsCollection = _database.GetCollection<Department>("departments");

            // "cities" tablosuna bağlanıyoruz
            _citiesCollection = _database.GetCollection<CityData>("cities");

            _environment = environment;
        }

        // ==========================================
        // 1. KAYIT (REGISTER)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Departments = await _departmentsCollection.Find(_ => true).ToListAsync();
            // Kayıt olurken de şehir seçtirmek istersen buraya ViewBag.Cities ekleyebilirsin
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
                    ModelState.AddModelError("Email", "Bu e-posta zaten kullanımda.");
                    ViewBag.Departments = await _departmentsCollection.Find(_ => true).ToListAsync();
                    return View(model);
                }

                var newUser = new User
                {
                    Name = model.Name.Trim(),
                    Email = model.Email.ToLower().Trim(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    ProfileImageUrl = "",
                    DepartmentId = model.DepartmentId
                };

                await _usersCollection.InsertOneAsync(newUser);
                TempData["SuccessMessage"] = "Kayıt başarılı. Giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }

            ViewBag.Departments = await _departmentsCollection.Find(_ => true).ToListAsync();
            return View(model);
        }

        // ==========================================
        // 2. GİRİŞ (LOGIN)
        // ==========================================
        [HttpGet]
        public IActionResult Login() => View();

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

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Hatalı e-posta veya şifre.");
            }
            return View(model);
        }

        // ==========================================
        // 3. ÇIKIŞ (LOGOUT)
        // ==========================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 4. PROFİL (GET)
        // ==========================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return NotFound();

            // Bölümleri al
            ViewBag.Departments = await _departmentsCollection.Find(_ => true).ToListAsync();

            // Şehirleri "cities" tablosundan al
            ViewBag.Cities = await _citiesCollection.Find(_ => true).ToListAsync();

            var model = new EditProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                DepartmentId = user.DepartmentId,
                CurrentProfileImageUrl = user.ProfileImageUrl,
                City = user.City,
                District = user.District,
                PreferredLocationsText = user.PreferredLocationsText
            };

            return View(model);
        }

        // ==========================================
        // 5. PROFİL (POST)
        // ==========================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null) return RedirectToAction("Login");

            // Şifre boşsa validasyon hatalarını temizle
            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.Remove(nameof(model.CurrentPassword));
                ModelState.Remove(nameof(model.NewPassword));
                ModelState.Remove(nameof(model.ConfirmNewPassword));
            }

            // Hata varsa sayfayı tekrar doldurup göster
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _departmentsCollection.Find(_ => true).ToListAsync();
                ViewBag.Cities = await _citiesCollection.Find(_ => true).ToListAsync();
                model.CurrentProfileImageUrl = user.ProfileImageUrl;
                return View(model);
            }

            // --- GÜNCELLEME AYARLARI ---
            var updateDef = Builders<User>.Update
                .Set(u => u.Name, model.Name.Trim())
                .Set(u => u.DepartmentId, model.DepartmentId)
                // Şehir ve İlçe verisini güvenli şekilde kaydediyoruz
                .Set(u => u.City, model.City)
                .Set(u => u.District, model.District)
                .Set(u => u.PreferredLocationsText, model.PreferredLocationsText?.Trim());

            // --- 1. PROFIL FOTOĞRAFI YÜKLEME KISMI ---
            if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
            {
                // Dosya adını benzersiz yap (User ID + Tarih)
                var extension = Path.GetExtension(model.ProfileImageFile.FileName).ToLower();
                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";

                // Klasör yolunu belirle (wwwroot/images/profiles)
                var path = Path.Combine(_environment.WebRootPath, "images", "profiles", fileName);

                // Klasör yoksa oluştur
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                // Dosyayı kaydet
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.ProfileImageFile.CopyToAsync(stream);
                }

                // Veritabanına dosya yolunu yaz
                updateDef = updateDef.Set(u => u.ProfileImageUrl, $"/images/profiles/{fileName}");
            }

            // --- 2. ŞİFRE GÜNCELLEME ---
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                updateDef = updateDef.Set(u => u.PasswordHash, BCrypt.Net.BCrypt.HashPassword(model.NewPassword));
            }

            // --- 3. VERİTABANINI GÜNCELLE ---
            await _usersCollection.UpdateOneAsync(u => u.Id == userId, updateDef);

            // Cookie'deki İsmi Güncelle
            var identity = (ClaimsIdentity)User.Identity;
            var nameClaim = identity.FindFirst(ClaimTypes.Name);
            if (nameClaim != null) identity.RemoveClaim(nameClaim);
            identity.AddClaim(new Claim(ClaimTypes.Name, model.Name));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            TempData["SuccessMessage"] = "Profil bilgilerin başarıyla kaydedildi.";
            return RedirectToAction("Profile");
        }
    }
}