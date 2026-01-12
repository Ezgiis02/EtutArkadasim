using EtutArkadasim.Web.Models;
using EtutArkadasim.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace EtutArkadasim.Web.Controllers
{
    // BU SATIRLAR API OLMASINI SAĞLAR
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Department> _departmentsCollection;

        public AuthController(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("users");
            _departmentsCollection = database.GetCollection<Department>("departments");
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // 1. Model geçerli mi kontrol et
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Eksik veya hatalı bilgi gönderildi." });

            // 2. Kullanıcıyı bul
            var user = await _usersCollection.Find(u => u.Email == model.Email.ToLower()).FirstOrDefaultAsync();

            // 3. Kullanıcı yoksa veya şifre yanlışsa
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });
            }

            // 4. BAŞARILI! Flutter'a JSON dönüyoruz.
            // NOT: İleride buraya JWT Token eklenmeli ama şimdilik ID dönüyoruz.
            return Ok(new
            {
                message = "Giriş Başarılı",
                userId = user.Id.ToString(),
                name = user.Name,
                email = user.Email,
                departmentId = user.DepartmentId
            });
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new { message = "E-posta ve şifre zorunludur." });
            }

            var existingUser = await _usersCollection.Find(u => u.Email == model.Email.ToLower()).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return Conflict(new { message = "Bu e-posta adresi zaten kullanılıyor." });
            }

            // Yeni kullanıcı oluştur
            var newUser = new User
            {
                Name = model.Name,
                Email = model.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                DepartmentId = model.DepartmentId,
                ProfileImageUrl = ""
            };

            await _usersCollection.InsertOneAsync(newUser);

            return Ok(new { message = "Kayıt başarılı", userId = newUser.Id.ToString() });
        }
    }
}