using EtutArkadasim.Web.Models;
using EtutArkadasim.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EtutArkadasim.Web.Controllers
{
    [Authorize]
    public class DiscoverController : Controller
    {
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Course> _coursesCollection;
        private readonly IMongoCollection<Department> _departmentsCollection;

        public DiscoverController(IMongoDatabase database)
        {
            _usersCollection = database.GetCollection<User>("users");
            _coursesCollection = database.GetCollection<Course>("courses");
            _departmentsCollection = database.GetCollection<Department>("departments");
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] List<string>? courseIds = null,
            [FromQuery] string? city = null,
            [FromQuery] string? district = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Kullanıcıyı Çek (Derslerini bilmemiz lazım)
            var currentUser = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // 2. Filtreleme
            var userFilter = Builders<User>.Filter.And(
                Builders<User>.Filter.Ne(u => u.Id, currentUserId),
                // KRİTİK NOKTA: Sadece benim derslerimden EN AZ BİRİNİ alanlar gelsin
                Builders<User>.Filter.AnyIn(u => u.SelectedCourseIds, currentUser.SelectedCourseIds)
            );

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                userFilter = Builders<User>.Filter.And(userFilter,
                    Builders<User>.Filter.Regex(u => u.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")));
            }
            if (!string.IsNullOrEmpty(city))
            {
                userFilter = Builders<User>.Filter.And(userFilter, Builders<User>.Filter.Eq(u => u.City, city));
            }
            if (!string.IsNullOrEmpty(district))
            {
                userFilter = Builders<User>.Filter.And(userFilter, Builders<User>.Filter.Eq(u => u.District, district));
            }
            // Ekstra ders filtresi seçildiyse onu da ekle
            if (courseIds != null && courseIds.Any())
            {
                userFilter = Builders<User>.Filter.And(userFilter,
                    Builders<User>.Filter.AnyIn(u => u.SelectedCourseIds, courseIds));
            }

            // Verileri çek
            var filteredUsers = await _usersCollection.Find(userFilter).ToListAsync();

            // 3. Bölüm İsimlerini Hazırla
            var departmentIds = filteredUsers.Select(u => u.DepartmentId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var departments = await _departmentsCollection.Find(d => departmentIds.Contains(d.Id)).ToListAsync();
            var deptDict = departments.ToDictionary(d => d.Id, d => d.DepartmentName);

            // 4. DERSLERİ HAZIRLA (Ortak dersleri bulmak için tüm dersleri çekip hafızaya alalım)
            // Performans için sadece ilgili dersleri çekebiliriz ama şimdilik tümünü çekmek daha güvenli
            var allCourses = await _coursesCollection.Find(_ => true).ToListAsync();
            var courseDict = allCourses.ToDictionary(c => c.Id);

            // 5. Model Doldurma (Ortak Ders Hesaplama)
            var userMatches = filteredUsers.Select(user => {

                // ORTAK DERSLERİ BUL (Intersection)
                var commonCourseIds = user.SelectedCourseIds.Intersect(currentUser.SelectedCourseIds).ToList();

                // ID listesini Course objesi listesine çevir
                var commonCoursesList = commonCourseIds
                    .Where(id => courseDict.ContainsKey(id))
                    .Select(id => courseDict[id])
                    .ToList();

                return new UserMatchViewModel
                {
                    User = user,
                    // Artık count'u listeden alıyoruz
                    CommonCoursesCount = commonCoursesList.Count,
                    // YENİ: Listeyi dolduruyoruz
                    CommonCourses = commonCoursesList,

                    CompatibilityScore = CalculateSimpleScore(user, city, district),
                    DepartmentName = (user.DepartmentId != null && deptDict.ContainsKey(user.DepartmentId))
                                     ? deptDict[user.DepartmentId]
                                     : "Mühendislik Öğrencisi"
                };
            })
            .OrderByDescending(m => m.CommonCoursesCount)
            .ThenByDescending(m => m.CompatibilityScore)
            .ToList();

            // Sayfalama
            var totalCount = userMatches.Count;
            var pagedUsers = userMatches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // ViewModel
            var viewModel = new DiscoverViewModel
            {
                Users = pagedUsers,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                AppliedFilters = new DiscoverFilters
                {
                    CourseIds = courseIds ?? new List<string>(),
                    SearchTerm = searchTerm,
                    Cities = !string.IsNullOrEmpty(city) ? new List<string> { city } : new List<string>(),
                    Districts = !string.IsNullOrEmpty(district) ? new List<string> { district } : new List<string>()
                }
            };

            return View(viewModel);
        }

        private double CalculateSimpleScore(User user, string? city, string? district)
        {
            double score = 0;
            if (!string.IsNullOrEmpty(city) && user.City == city) score += 50;
            if (!string.IsNullOrEmpty(district) && user.District == district) score += 50;
            return score;
        }
    }
}