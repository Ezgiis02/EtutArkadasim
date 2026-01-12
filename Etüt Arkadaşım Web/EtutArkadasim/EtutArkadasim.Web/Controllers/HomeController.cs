using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Diagnostics;
using EtutArkadasim.Web.ViewModels; // ViewModel için gerekebilir

namespace EtutArkadasim.Web.Controllers
{
    [Authorize] // Bu controller'a sadece giriş yapanlar girebilir
    public class HomeController : Controller
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Course> _coursesCollection; // Dersler için
        private readonly IMongoCollection<StudyRequest> _requestsCollection;

        public HomeController(IMongoDatabase database)
        {
            _database = database;
            _usersCollection = _database.GetCollection<User>("users");
            _coursesCollection = _database.GetCollection<Course>("courses"); // Bağlantı
            _requestsCollection = _database.GetCollection<StudyRequest>("studyRequests");
        }

        public IActionResult Index()
        {
            // Ana sayfa (Login olmamış kullanıcılar için tanıtım sayfası olabilir)
            // Eğer giriş yapmışsa Dashboard'a yönlendirsin
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _usersCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();

            if (user == null) return RedirectToAction("Login", "Account");

            // --- A. MEVCUT DERS LİSTESİ İŞLEMLERİ (ESKİ KODUN) ---
            var userCourses = new List<Course>();
            if (user.SelectedCourseIds != null && user.SelectedCourseIds.Any())
            {
                var filter = Builders<Course>.Filter.In(c => c.Id, user.SelectedCourseIds);
                userCourses = await _coursesCollection.Find(filter).ToListAsync();
            }
            ViewBag.UserCourses = userCourses;

            var allCourses = await _coursesCollection.Find(_ => true).ToListAsync();
            var availableCourses = allCourses.Where(c => !user.SelectedCourseIds.Contains(c.Id)).ToList();
            ViewBag.AvailableCourses = availableCourses;

            // --- B. GELEN BEKLEYEN İSTEKLERİ ÇEK ---
            // ReceiverId = BEN ve Status = Pending olanlar
            var pendingRequests = await _requestsCollection.Find(r => r.ReceiverId == userId && r.Status == RequestStatus.Pending).ToListAsync();

            // İstek gönderenlerin detaylarını (İsim, Resim) bulmak için User tablosuna gidiyoruz
            var pendingRequestViewModels = new List<dynamic>();
            foreach (var req in pendingRequests)
            {
                var sender = await _usersCollection.Find(u => u.Id == req.SenderId).FirstOrDefaultAsync();
                var course = await _coursesCollection.Find(c => c.Id == req.CourseId).FirstOrDefaultAsync();
                if (sender != null)
                {
                    // View'da kullanmak için dinamik bir obje oluşturuyoruz
                    pendingRequestViewModels.Add(new
                    {
                        RequestId = req.Id,
                        SenderName = sender.Name,
                        SenderImage = sender.ProfileImageUrl,
                        CourseName = course?.CourseName ?? "Bilinmeyen Ders",
                        SentAt = req.RequestedAt
                    });
                }
            }
            ViewBag.PendingRequests = pendingRequestViewModels;

            // --- C. ARKADAŞLARI (KABUL EDİLMİŞ İSTEKLERİ) ÇEK ---
            // (Benim gönderdiğim VE kabul edilenler) VEYA (Bana gelen VE kabul edilenler)
            var acceptedRequests = await _requestsCollection.Find(r =>
                r.Status == RequestStatus.Accepted &&
                (r.SenderId == userId || r.ReceiverId == userId)
            ).ToListAsync();

            var friendIds = new List<string>();
            foreach (var req in acceptedRequests)
            {
                // Eğer gönderen bensem, arkadaşım alıcıdır. Alıcı bensem, arkadaşım gönderendir.
                if (req.SenderId == userId) friendIds.Add(req.ReceiverId);
                else friendIds.Add(req.SenderId);
            }
            // Tekrar edenleri temizle (Distinct)
            friendIds = friendIds.Distinct().ToList();

            var friends = new List<User>();
            if (friendIds.Any())
            {
                friends = await _usersCollection.Find(Builders<User>.Filter.In(u => u.Id, friendIds)).ToListAsync();
            }
            ViewBag.Friends = friends;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> AddCourse(string courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(courseId))
            {
                // Kullanıcının SelectedCourseIds listesine ekle
                var update = Builders<User>.Update.AddToSet(u => u.SelectedCourseIds, courseId);
                await _usersCollection.UpdateOneAsync(u => u.Id == userId, update);
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveCourse(string courseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(courseId))
            {
                // Kullanıcının listesinden çıkar (Pull)
                var update = Builders<User>.Update.Pull(u => u.SelectedCourseIds, courseId);
                await _usersCollection.UpdateOneAsync(u => u.Id == userId, update);
            }

            return RedirectToAction("Dashboard");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}