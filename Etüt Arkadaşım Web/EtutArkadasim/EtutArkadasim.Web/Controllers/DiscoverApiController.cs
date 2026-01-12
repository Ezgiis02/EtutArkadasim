using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Controllers.Api
{
    [Route("api/discover")]
    [ApiController]
    public class DiscoverApiController : ControllerBase
    {
        private readonly IMongoCollection<User> _usersCollection;
        // YENİ: Bölüm koleksiyonunu ekledik
        private readonly IMongoCollection<Department> _departmentsCollection;

        public DiscoverApiController(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("users");
            // YENİ: Başlatıyoruz
            _departmentsCollection = database.GetCollection<Department>("departments");
        }

        // GET: api/discover/search
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? city,
            [FromQuery] string? district,
            [FromQuery] string? currentUserId)
        {
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized(new { message = "User ID gerekli." });

            var currentUser = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
            if (currentUser == null) return NotFound("Kullanıcı bulunamadı.");

            if (currentUser.SelectedCourseIds == null || !currentUser.SelectedCourseIds.Any())
            {
                return Ok(new { matches = new List<object>() });
            }

            // --- FİLTRELEME ---
            var userFilter = Builders<User>.Filter.Ne(u => u.Id, currentUserId);

            if (currentUser.FavoriteUserIds != null && currentUser.FavoriteUserIds.Any())
            {
                userFilter = Builders<User>.Filter.And(userFilter, Builders<User>.Filter.Nin(u => u.Id, currentUser.FavoriteUserIds));
            }

            userFilter = Builders<User>.Filter.And(userFilter, Builders<User>.Filter.AnyIn(u => u.SelectedCourseIds, currentUser.SelectedCourseIds));

            if (!string.IsNullOrEmpty(city))
            {
                userFilter = Builders<User>.Filter.And(userFilter, Builders<User>.Filter.Eq(u => u.City, city));
            }

            var users = await _usersCollection.Find(userFilter).ToListAsync();

            // YENİ ADIM: Bölüm isimlerini çekiyoruz (Performans için toplu çekim)
            var departmentIds = users.Select(u => u.DepartmentId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var departments = await _departmentsCollection.Find(d => departmentIds.Contains(d.Id)).ToListAsync();
            var deptDict = departments.ToDictionary(d => d.Id, d => d.DepartmentName);

            // JSON PAKETLEME
            var matches = users.Select(u => new
            {
                id = u.Id.ToString(),
                name = u.Name,
                email = u.Email,
                profileImageUrl = u.ProfileImageUrl,
                rating = u.AverageRating,
                courses = u.SelectedCourseIds,
                city = u.City,
                district = u.District,

                // YENİ: Bölüm Adını ekliyoruz
                departmentName = (u.DepartmentId != null && deptDict.ContainsKey(u.DepartmentId))
                                 ? deptDict[u.DepartmentId]
                                 : "Mühendislik Öğrencisi"
            }).ToList();

            return Ok(new { matches = matches });
        }
    }
}