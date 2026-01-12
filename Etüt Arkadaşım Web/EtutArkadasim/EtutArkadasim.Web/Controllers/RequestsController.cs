using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace EtutArkadasim.Web.Controllers
{
    // BU CONTROLLER SADECE İSTEK VE ARKADAŞLIK YÖNETİMİ İÇİNDİR
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly IMongoCollection<StudyRequest> _requestsCollection;
        private readonly IMongoCollection<User> _usersCollection;

        public RequestsController(IMongoDatabase database)
        {
            _requestsCollection = database.GetCollection<StudyRequest>("studyRequests");
            _usersCollection = database.GetCollection<User>("users");
        }

        // --- 1. İSTEK GÖNDER (Web Sitesi Keşfet Sayfasından) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRequest(string receiverId, string courseId)
        {
            var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Basit Kontroller
            if (string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(courseId))
            {
                TempData["ErrorMessage"] = "Eksik bilgi: Ders seçimi yapılmadı.";
                return RedirectToAction("Index", "Discover");
            }

            if (senderId == receiverId)
            {
                TempData["ErrorMessage"] = "Kendinize istek gönderemezsiniz.";
                return RedirectToAction("Index", "Discover");
            }

            // Zaten bekleyen istek var mı?
            var existingRequest = await _requestsCollection.Find(r =>
                r.Status == RequestStatus.Pending &&
                r.CourseId == courseId &&
                ((r.SenderId == senderId && r.ReceiverId == receiverId) ||
                 (r.SenderId == receiverId && r.SenderId == senderId))
            ).FirstOrDefaultAsync();

            if (existingRequest != null)
            {
                TempData["ErrorMessage"] = "Bu kişiye bu ders için zaten bekleyen bir talebiniz var.";
                return RedirectToAction("Index", "Discover");
            }

            // Kaydet
            var newRequest = new StudyRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                CourseId = courseId,
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            await _requestsCollection.InsertOneAsync(newRequest);

            TempData["SuccessMessage"] = "Çalışma isteği başarıyla gönderildi!";
            return RedirectToAction("Index", "Discover");
        }

        // --- 2. İSTEĞİ KABUL ET ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(string requestId)
        {
            var request = await _requestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();
            if (request == null) return NotFound();

            // Durumu 'Accepted' yap -> Artık Dashboard'da arkadaş olarak görünecekler
            var update = Builders<StudyRequest>.Update.Set(r => r.Status, RequestStatus.Accepted);
            await _requestsCollection.UpdateOneAsync(r => r.Id == requestId, update);

            TempData["SuccessMessage"] = "Çalışma isteğini kabul ettin! Artık arkadaşsınız.";
            return RedirectToAction("Dashboard", "Home");
        }

        // --- 3. İSTEĞİ REDDET ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(string requestId)
        {
            var update = Builders<StudyRequest>.Update.Set(r => r.Status, RequestStatus.Rejected);
            await _requestsCollection.UpdateOneAsync(r => r.Id == requestId, update);

            TempData["SuccessMessage"] = "İstek reddedildi.";
            return RedirectToAction("Dashboard", "Home");
        }

        // --- 4. FAVORİLE (KALP BUTONU) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(string friendId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();

            if (currentUser.FavoriteUserIds.Contains(friendId))
            {
                // Zaten favoriyse çıkar (Kalbi boşalt)
                var update = Builders<User>.Update.Pull(u => u.FavoriteUserIds, friendId);
                await _usersCollection.UpdateOneAsync(u => u.Id == currentUserId, update);
                TempData["SuccessMessage"] = "Favorilerden çıkarıldı.";
            }
            else
            {
                // Değilse ekle (Kalbi doldur)
                var update = Builders<User>.Update.AddToSet(u => u.FavoriteUserIds, friendId);
                await _usersCollection.UpdateOneAsync(u => u.Id == currentUserId, update);
                TempData["SuccessMessage"] = "Favorilere eklendi!";
            }

            return RedirectToAction("Dashboard", "Home");
        }

        // --- 5. ARKADAŞ SİL (ÇÖP KUTUSU) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Varsa favorilerden karşılıklı çıkar (Temizlik)
            var updateFriend = Builders<User>.Update.Pull(u => u.FavoriteUserIds, currentUserId);
            await _usersCollection.UpdateOneAsync(u => u.Id == friendId, updateFriend);

            var updateMe = Builders<User>.Update.Pull(u => u.FavoriteUserIds, friendId);
            await _usersCollection.UpdateOneAsync(u => u.Id == currentUserId, updateMe);

            // 2. Esas İşlem: Aradaki "Accepted" olan çalışma isteğini SİL.
            // Bu istek silindiği an, Dashboard'daki "Arkadaşlarım" listesinden düşerler.
            await _requestsCollection.DeleteManyAsync(r =>
                (r.SenderId == currentUserId && r.ReceiverId == friendId) ||
                (r.SenderId == friendId && r.ReceiverId == currentUserId));

            TempData["SuccessMessage"] = "Arkadaş silindi.";
            return RedirectToAction("Dashboard", "Home");
        }
    }
}