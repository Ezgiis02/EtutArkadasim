using EtutArkadasim.Web.Models;
using EtutArkadasim.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace EtutArkadasim.Web.Controllers
{
    [Route("api/request")]
    [ApiController]
    [AllowAnonymous] // Kilitleri kaldırıyoruz, içeride hibrit kontrol yapacağız
    public class StudyRequestController : ControllerBase
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<StudyRequest> _requestsCollection;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<Course> _coursesCollection;
        private readonly IMongoCollection<StudyPreferences> _preferencesCollection;
        private readonly IMongoCollection<StudySchedule> _scheduleCollection;

        public StudyRequestController(IMongoDatabase database)
        {
            _database = database;
            _requestsCollection = _database.GetCollection<StudyRequest>("studyRequests");
            _usersCollection = _database.GetCollection<User>("users");
            _preferencesCollection = _database.GetCollection<StudyPreferences>("studyPreferences");
            _scheduleCollection = _database.GetCollection<StudySchedule>("studySchedules");
            _coursesCollection = database.GetCollection<Course>("courses");
        }

        // --- YARDIMCI METOD: HEM COOKIE HEM URL PARAMETRESİNİ KONTROL ET ---
        private string? GetEffectiveUserId(string? queryUserId)
        {
            // 1. Web'den geliyorsa Cookie'ye bak
            var cookieUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(cookieUserId)) return cookieUserId;

            // 2. Mobilden geliyorsa URL parametresine bak
            if (!string.IsNullOrEmpty(queryUserId)) return queryUserId;

            return null;
        }

        // --- 1. İSTEK GÖNDER ---
        [HttpPost("send/{receiverId}/{courseId}")]
        public async Task<IActionResult> SendRequest(string receiverId, string courseId, [FromQuery] string? userId)
        {
            var senderId = GetEffectiveUserId(userId); // HİBRİT KİMLİK KONTROLÜ
            if (string.IsNullOrEmpty(senderId)) return Unauthorized();

            if (senderId == receiverId) return BadRequest(new { message = "Kendinize istek atamazsınız." });

            var receiverExists = await _usersCollection.Find(u => u.Id == receiverId).AnyAsync();
            if (!receiverExists) return NotFound(new { message = "Kullanıcı bulunamadı." });

            var courseExists = await _coursesCollection.Find(c => c.Id == courseId).AnyAsync();
            if (!courseExists) return NotFound(new { message = "Ders bulunamadı." });

            // Zaten bekleyen istek var mı?
            var existing = await _requestsCollection.Find(r =>
                r.Status == RequestStatus.Pending && r.CourseId == courseId &&
                ((r.SenderId == senderId && r.ReceiverId == receiverId) ||
                 (r.SenderId == receiverId && r.ReceiverId == senderId))
            ).FirstOrDefaultAsync();

            if (existing != null) return BadRequest(new { message = "Zaten bekleyen bir talep var." });

            // Zaten arkadaş mı?
            var senderUser = await _usersCollection.Find(u => u.Id == senderId).FirstOrDefaultAsync();
            var receiverUser = await _usersCollection.Find(u => u.Id == receiverId).FirstOrDefaultAsync(); // receiverUser'ı da çekelim

            if (senderUser.FavoriteUserIds.Contains(receiverId)) return BadRequest(new { message = "Zaten arkadaşsınız." });

            // Ders seçimi kontrolü (Sizin eski kodunuzdaki mantık)
            if (senderUser != null && receiverUser != null)
            {
                if (!senderUser.SelectedCourseIds.Contains(courseId) || !receiverUser.SelectedCourseIds.Contains(courseId))
                {
                    return BadRequest(new { message = "Hem sizin hem de alıcının bu dersi seçmiş olması gerekmektedir." });
                }
            }

            // UYUMLULUK KONTROLÜ
            var senderPreferences = await _preferencesCollection.Find(p => p.UserId == senderId).FirstOrDefaultAsync();
            var receiverPreferences = await _preferencesCollection.Find(p => p.UserId == receiverId).FirstOrDefaultAsync();
            var senderSchedule = await _scheduleCollection.Find(s => s.UserId == senderId).FirstOrDefaultAsync();
            var receiverSchedule = await _scheduleCollection.Find(s => s.UserId == receiverId).FirstOrDefaultAsync();

            if (senderPreferences != null && receiverPreferences != null && senderSchedule != null && receiverSchedule != null)
            {
                var compatibilityScore = CalculateStudyCompatibility(senderPreferences, senderSchedule, receiverPreferences, receiverSchedule);
                if (compatibilityScore < 30)
                {
                    return BadRequest(new { message = $"Uyumluluk skoru çok düşük: %{compatibilityScore:F1}" });
                }
            }

            var newRequest = new StudyRequest
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Status = RequestStatus.Pending,
                RequestedAt = DateTime.UtcNow,
                CourseId = courseId
            };

            await _requestsCollection.InsertOneAsync(newRequest);
            return Ok(new { message = "İstek gönderildi." });
        }

        // --- 2. BEKLEYEN İSTEKLERİ LİSTELE ---
        [HttpGet("pending")]
        public async Task<IActionResult> GetMyPendingRequests([FromQuery] string? userId)
        {
            var currentUserId = GetEffectiveUserId(userId);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var pendingRequests = await _requestsCollection.Find(
                r => r.ReceiverId == currentUserId && r.Status == RequestStatus.Pending
            ).ToListAsync();

            if (!pendingRequests.Any()) return Ok(new List<PendingRequestViewModel>());

            var senderIds = pendingRequests.Select(r => r.SenderId).ToList();
            var senders = await _usersCollection.Find(u => senderIds.Contains(u.Id)).ToListAsync();

            var results = pendingRequests.Select(req => {
                var sender = senders.FirstOrDefault(s => s.Id == req.SenderId);
                return new PendingRequestViewModel
                {
                    RequestId = req.Id,
                    SenderId = req.SenderId,
                    SenderName = sender?.Name ?? "Bilinmeyen Kullanıcı",
                    RequestedAt = req.RequestedAt
                };
            }).ToList();

            return Ok(results);
        }

        // --- 3. İSTEĞİ KABUL ET ---
        [HttpPost("accept/{requestId}")]
        public async Task<IActionResult> AcceptRequest(string requestId, [FromQuery] string? userId)
        {
            var currentUserId = GetEffectiveUserId(userId);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var request = await _requestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();
            if (request == null) return NotFound(new { message = "Talep bulunamadı." });

            if (request.ReceiverId != currentUserId) return Unauthorized(new { message = "Bu talebi işleme yetkiniz yok." });
            if (request.Status != RequestStatus.Pending) return BadRequest(new { message = "Talep zaten işlenmiş." });

            // Durumu güncelle
            var updateRequest = Builders<StudyRequest>.Update.Set(r => r.Status, RequestStatus.Accepted);
            await _requestsCollection.UpdateOneAsync(r => r.Id == requestId, updateRequest);

            // Arkadaş ekle
            var senderId = request.SenderId;
            var updateReceiver = Builders<User>.Update.AddToSet(u => u.FavoriteUserIds, senderId);
            await _usersCollection.UpdateOneAsync(u => u.Id == currentUserId, updateReceiver);

            var updateSender = Builders<User>.Update.AddToSet(u => u.FavoriteUserIds, currentUserId);
            await _usersCollection.UpdateOneAsync(u => u.Id == senderId, updateSender);

            return Ok(new { message = "Talep kabul edildi." });
        }

        // --- 4. İSTEĞİ REDDET ---
        [HttpPost("reject/{requestId}")]
        public async Task<IActionResult> RejectRequest(string requestId, [FromQuery] string? userId)
        {
            var currentUserId = GetEffectiveUserId(userId);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var request = await _requestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();
            if (request == null) return NotFound(new { message = "Talep bulunamadı." });

            if (request.ReceiverId != currentUserId) return Unauthorized();

            var updateRequest = Builders<StudyRequest>.Update.Set(r => r.Status, RequestStatus.Rejected);
            await _requestsCollection.UpdateOneAsync(r => r.Id == requestId, updateRequest);

            return Ok(new { message = "Talep reddedildi." });
        }

        // --- 5. ARKADAŞLARI LİSTELE ---
        [HttpGet("myfriends")]
        public async Task<IActionResult> GetMyFriends([FromQuery] string? userId)
        {
            var currentUserId = GetEffectiveUserId(userId);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var user = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();

            if (user == null || !user.FavoriteUserIds.Any())
            {
                return Ok(new List<FriendViewModel>());
            }

            var filter = Builders<User>.Filter.In(u => u.Id, user.FavoriteUserIds);
            var friends = await _usersCollection.Find(filter).ToListAsync();

            var friendViewModels = friends.Select(f => new FriendViewModel
            {
                Id = f.Id,
                Name = f.Name,
                ProfileImageUrl = f.ProfileImageUrl,
                AverageRating = f.AverageRating
            }).ToList();

            return Ok(friendViewModels);
        }

        // --- 6. ARKADAŞ SİL ---
        [HttpDelete("removefriend/{friendId}")]
        public async Task<IActionResult> RemoveFriend(string friendId, [FromQuery] string? userId)
        {
            var currentUserId = GetEffectiveUserId(userId);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var user = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
            if (user == null || !user.FavoriteUserIds.Contains(friendId))
            {
                return BadRequest(new { message = "Bu kullanıcı arkadaşınız değil." });
            }

            // Karşılıklı Silme
            var updateCurrentUser = Builders<User>.Update.Pull(u => u.FavoriteUserIds, friendId);
            await _usersCollection.UpdateOneAsync(u => u.Id == currentUserId, updateCurrentUser);

            var updateFriend = Builders<User>.Update.Pull(u => u.FavoriteUserIds, currentUserId);
            await _usersCollection.UpdateOneAsync(u => u.Id == friendId, updateFriend);

            // İlişkili kabul edilmiş talepleri de temizle
            var acceptedRequests = await _requestsCollection.Find(
                r => r.Status == RequestStatus.Accepted &&
                ((r.SenderId == currentUserId && r.ReceiverId == friendId) ||
                 (r.SenderId == friendId && r.ReceiverId == currentUserId))
            ).ToListAsync();

            if (acceptedRequests.Any())
            {
                var requestIds = acceptedRequests.Select(r => r.Id).ToList();
                await _requestsCollection.DeleteManyAsync(r => requestIds.Contains(r.Id));
            }

            return Ok(new { message = "Arkadaşlıktan çıkarıldı." });
        }

        // --- 7. EŞLEŞMEYİ SONLANDIR (Yukarıdakiyle benzer mantık) ---
        [HttpDelete("endmatch/{friendId}")]
        public async Task<IActionResult> EndMatch(string friendId, [FromQuery] string? userId)
        {
            return await RemoveFriend(friendId, userId); // Aynı işlemi yaptığı için tekrar yazmadım, yönlendirdim
        }


        // --- YARDIMCI HESAPLAMA METODLARI ---
        private double CalculateStudyCompatibility(StudyPreferences prefs1, StudySchedule sched1, StudyPreferences prefs2, StudySchedule sched2)
        {
            double score = 0.0;
            int totalCriteria = 0;

            // Zaman Uyumluluğu
            if (prefs1.PreferredStudyTimes.Any() && prefs2.PreferredStudyTimes.Any())
            {
                totalCriteria++;
                if (prefs1.PreferredStudyTimes.Any(t => prefs2.PreferredStudyTimes.Contains(t))) score += 25;
            }

            // Ortam Uyumluluğu
            if (prefs1.PreferredEnvironments.Any() && prefs2.PreferredEnvironments.Any())
            {
                totalCriteria++;
                if (prefs1.PreferredEnvironments.Any(e => prefs2.PreferredEnvironments.Contains(e))) score += 25;
            }

            // Format Uyumluluğu
            if (prefs1.PreferredFormats.Any() && prefs2.PreferredFormats.Any())
            {
                totalCriteria++;
                if (prefs1.PreferredFormats.Any(f => prefs2.PreferredFormats.Contains(f))) score += 25;
            }

            // Konum Uyumluluğu (Şehir/İlçe/Mekan)
            bool locationMatch = false;
            if (prefs1.PreferredCities.Any(c => prefs2.PreferredCities.Contains(c)) ||
                prefs1.PreferredDistricts.Any(d => prefs2.PreferredDistricts.Contains(d)) ||
                prefs1.PreferredLocations.Any(l => prefs2.PreferredLocations.Contains(l)))
            {
                locationMatch = true;
            }

            if (locationMatch)
            {
                totalCriteria++;
                score += 25;
            }

            // Motivasyon (Ekstra Puan)
            if (prefs1.MotivationStyle == prefs2.MotivationStyle) score += 10;

            // Haftalık Takvim Çakışması (Ekstra Puan)
            if (sched1 != null && sched2 != null)
            {
                var commonSlots = GetCommonAvailableSlots(sched1, sched2);
                score += Math.Min(commonSlots * 5, 20);
            }

            return score;
        }

        private int GetCommonAvailableSlots(StudySchedule sched1, StudySchedule sched2)
        {
            int commonSlots = 0;
            foreach (var day1 in sched1.WeeklySchedule.Where(d => d.IsAvailable))
            {
                var day2 = sched2.WeeklySchedule.FirstOrDefault(d => (int)d.Day == (int)day1.Day && d.IsAvailable);
                if (day2 != null)
                {
                    commonSlots += day1.TimeSlots.Count(t1 =>
                        day2.TimeSlots.Any(t2 =>
                            t1.IsAvailable && t2.IsAvailable &&
                            TimeSlotsOverlap(t1.StartTime, t1.EndTime, t2.StartTime, t2.EndTime)));
                }
            }
            return commonSlots;
        }

        private bool TimeSlotsOverlap(string start1, string end1, string start2, string end2)
        {
            if (!TimeSpan.TryParse(start1, out var s1) || !TimeSpan.TryParse(end1, out var e1) ||
                !TimeSpan.TryParse(start2, out var s2) || !TimeSpan.TryParse(end2, out var e2))
                return false;

            return s1 < e2 && s2 < e1;
        }
    }
}