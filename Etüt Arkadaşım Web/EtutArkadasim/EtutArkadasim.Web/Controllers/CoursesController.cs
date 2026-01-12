using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Security.Claims; // Cookie için gerekli
using System.Threading.Tasks;

namespace EtutArkadasim.Web.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous] // Kapıyı herkese açıyoruz, içeride kimlik kontrolü yapacağız
    public class CoursesController : ControllerBase
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Course> _coursesCollection;
        private readonly IMongoCollection<User> _usersCollection;

        public CoursesController(IMongoDatabase database)
        {
            _database = database;
            _coursesCollection = _database.GetCollection<Course>("courses");
            _usersCollection = _database.GetCollection<User>("users");
        }

        // --- YARDIMCI METOD: ID BULUCU ---
        // Bu metod, isteğin Web'den mi yoksa Mobilden mi geldiğine bakıp doğru ID'yi verir.
        private string? GetEffectiveUserId(string? queryUserId)
        {
            // 1. Önce Cookie'ye bak (Web için)
            var cookieUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(cookieUserId))
            {
                return cookieUserId;
            }

            // 2. Cookie yoksa Parametreye bak (Mobil için)
            if (!string.IsNullOrEmpty(queryUserId))
            {
                return queryUserId;
            }

            // 3. İkisi de yoksa kimliksizdir
            return null;
        }

        // --- 1. TÜM DERSLERİ GETİR ---
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllCourses()
        {
            // Herkes görebilir
            var courses = await _coursesCollection.Find(_ => true)
                .SortBy(c => c.Department)
                .ThenBy(c => c.CourseName)
                .ToListAsync();
            return Ok(courses);
        }

        // --- 2. BENİM DERSLERİMİ GETİR ---
        [HttpGet("getmycourses")]
        public async Task<IActionResult> GetMyCourses([FromQuery] string? userId)
        {
            // Hem Cookie'yi hem URL parametresini kontrol et
            var effectiveId = GetEffectiveUserId(userId);

            if (string.IsNullOrEmpty(effectiveId)) return Unauthorized(new { message = "Giriş yapmalısınız." });

            var user = await _usersCollection.Find(u => u.Id == effectiveId).FirstOrDefaultAsync();

            if (user == null || user.SelectedCourseIds == null || !user.SelectedCourseIds.Any())
            {
                return Ok(new List<Course>());
            }

            var filter = Builders<Course>.Filter.In(c => c.Id, user.SelectedCourseIds);
            var myCourses = await _coursesCollection.Find(filter).ToListAsync();
            return Ok(myCourses);
        }

        // --- 3. DERS EKLE ---
        [HttpPost("add/{courseId}")]
        public async Task<IActionResult> AddCourse(string courseId, [FromQuery] string? userId)
        {
            var effectiveId = GetEffectiveUserId(userId);
            if (string.IsNullOrEmpty(effectiveId)) return Unauthorized();

            // Ders var mı kontrolü
            var courseExists = await _coursesCollection.Find(c => c.Id == courseId).AnyAsync();
            if (!courseExists) return NotFound(new { message = "Ders bulunamadı." });

            var update = Builders<User>.Update.AddToSet(u => u.SelectedCourseIds, courseId);
            await _usersCollection.UpdateOneAsync(u => u.Id == effectiveId, update);

            return Ok(new { message = "Ders eklendi." });
        }

        // --- 4. DERS SİL ---
        [HttpDelete("remove/{courseId}")]
        public async Task<IActionResult> RemoveCourse(string courseId, [FromQuery] string? userId)
        {
            var effectiveId = GetEffectiveUserId(userId);
            if (string.IsNullOrEmpty(effectiveId)) return Unauthorized();

            var update = Builders<User>.Update.Pull(u => u.SelectedCourseIds, courseId);
            await _usersCollection.UpdateOneAsync(u => u.Id == effectiveId, update);

            return Ok(new { message = "Ders silindi." });
        }
    }
}