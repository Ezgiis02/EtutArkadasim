using EtutArkadasim.Web.Models;
using EtutArkadasim.Web.ViewModels; // PendingRequestViewModel ve FriendViewModel için
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Collections.Generic; // List için
using System.Linq; // .Select, .ToList için

[Route("api/request")] // Rota: /api/request
[ApiController]
[Authorize] // Bu API'ları sadece giriş yapanlar kullanabilir
public class StudyRequestController : ControllerBase
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<StudyRequest> _requestsCollection;
    private readonly IMongoCollection<User> _usersCollection;

    public StudyRequestController(IMongoDatabase database)
    {
        _database = database;
        _requestsCollection = _database.GetCollection<StudyRequest>("studyRequests");
        _usersCollection = _database.GetCollection<User>("users");
    }

    // --- API ENDPOINT 4: TALEP GÖNDER (4. Hafta) ---
    [HttpPost("send/{receiverId}")]
    public async Task<IActionResult> SendRequest(string receiverId)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId))
        {
            return Unauthorized();
        }
        if (senderId == receiverId)
        {
            return BadRequest(new { message = "Kullanıcı kendi kendine arkadaşlık isteği gönderemez." });
        }
        var receiverExists = await _usersCollection.Find(u => u.Id == receiverId).AnyAsync();
        if (!receiverExists)
        {
            return NotFound(new { message = "Talep gönderilecek kullanıcı bulunamadı." });
        }
        var existingPendingRequest = await _requestsCollection.Find(
            r => r.Status == RequestStatus.Pending &&
            ((r.SenderId == senderId && r.ReceiverId == receiverId) ||
             (r.SenderId == receiverId && r.ReceiverId == senderId))
        ).FirstOrDefaultAsync();

        if (existingPendingRequest != null)
        {
            return BadRequest(new { message = "Bu kullanıcıyla zaten beklemede olan bir talebiniz var." });
        }
        var newRequest = new StudyRequest
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Status = RequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };
        await _requestsCollection.InsertOneAsync(newRequest);
        return Ok(new { message = "Arkadaşlık talebi başarıyla gönderildi." });
    }

    // --- API ENDPOINT 5: BEKLEYEN TALEPLERİMİ LİSTELE (5. Hafta) ---
    [HttpGet("pending")]
    public async Task<IActionResult> GetMyPendingRequests()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }
        var pendingRequests = await _requestsCollection.Find(
            r => r.ReceiverId == currentUserId && r.Status == RequestStatus.Pending
        ).ToListAsync();

        if (!pendingRequests.Any())
        {
            return Ok(new List<PendingRequestViewModel>());
        }
        var senderIds = pendingRequests.Select(r => r.SenderId).ToList();
        var senders = await _usersCollection.Find(
            u => senderIds.Contains(u.Id)
        ).ToListAsync();

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

    // --- API ENDPOINT 6: TALEBİ KABUL ET (5. Hafta) ---
    [HttpPost("accept/{requestId}")]
    public async Task<IActionResult> AcceptRequest(string requestId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var request = await _requestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound(new { message = "Talep bulunamadı." });
        }
        if (request.ReceiverId != currentUserId)
        {
            return Unauthorized(new { message = "Bu talebi işleme yetkiniz yok." });
        }
        if (request.Status != RequestStatus.Pending)
        {
            return BadRequest(new { message = "Bu talep artık beklemede değil." });
        }

        var updateRequest = Builders<StudyRequest>.Update.Set(r => r.Status, RequestStatus.Accepted);
        await _requestsCollection.UpdateOneAsync(r => r.Id == requestId, updateRequest);

        var senderId = request.SenderId;
        var receiverId = currentUserId;

        var updateReceiver = Builders<User>.Update.AddToSet(u => u.FavoriteUserIds, senderId);
        await _usersCollection.UpdateOneAsync(u => u.Id == receiverId, updateReceiver);

        var updateSender = Builders<User>.Update.AddToSet(u => u.FavoriteUserIds, receiverId);
        await _usersCollection.UpdateOneAsync(u => u.Id == senderId, updateSender);

        return Ok(new { message = "Talep kabul edildi ve eşleşme sağlandı." });
    }

    // --- API ENDPOINT 7: TALEBİ REDDET (5. Hafta) ---
    [HttpPost("reject/{requestId}")]
    public async Task<IActionResult> RejectRequest(string requestId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var request = await _requestsCollection.Find(r => r.Id == requestId).FirstOrDefaultAsync();

        if (request == null)
        {
            return NotFound(new { message = "Talep bulunamadı." });
        }
        if (request.ReceiverId != currentUserId)
        {
            return Unauthorized(new { message = "Bu talebi işleme yetkiniz yok." });
        }
        if (request.Status != RequestStatus.Pending)
        {
            return BadRequest(new { message = "Bu talep artık beklemede değil." });
        }

        var updateRequest = Builders<StudyRequest>.Update.Set(r => r.Status, RequestStatus.Rejected);
        await _requestsCollection.UpdateOneAsync(r => r.Id == requestId, updateRequest);

        return Ok(new { message = "Talep reddedildi." });
    }

    // --- API ENDPOINT 8: ARKADAŞLARIMI LİSTELE (5. Hafta) ---
    [HttpGet("myfriends")]
    public async Task<IActionResult> GetMyFriends()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

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
            AverageRating = f.AverageRating
        }).ToList();

        return Ok(friendViewModels);
    }
}