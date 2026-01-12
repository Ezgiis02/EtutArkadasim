using EtutArkadasim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;

[Route("api/chat")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<ChatMessage> _chatMessagesCollection;
    private readonly IMongoCollection<User> _usersCollection;

    public ChatController(IMongoDatabase database)
    {
        _database = database;
        _chatMessagesCollection = _database.GetCollection<ChatMessage>("chatMessages");
        _usersCollection = _database.GetCollection<User>("users");
    }

    // --- API ENDPOINT: MESAJ GÖNDER ---
    [HttpPost("send/{receiverId}")]
    public async Task<IActionResult> SendMessage(string receiverId, [FromBody] SendMessageRequest request)
    {
        var senderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(senderId))
        {
            return Unauthorized();
        }

        // Kullanıcının arkadaş olup olmadığını kontrol et
        var sender = await _usersCollection.Find(u => u.Id == senderId).FirstOrDefaultAsync();
        if (sender == null || !sender.FavoriteUserIds.Contains(receiverId))
        {
            return BadRequest(new { message = "Sadece arkadaşlarınıza mesaj gönderebilirsiniz." });
        }

        var receiverExists = await _usersCollection.Find(u => u.Id == receiverId).AnyAsync();
        if (!receiverExists)
        {
            return NotFound(new { message = "Alıcı bulunamadı." });
        }

        var message = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Message = request.Message,
            SentAt = DateTime.UtcNow
        };

        await _chatMessagesCollection.InsertOneAsync(message);

        return Ok(new { message = "Mesaj başarıyla gönderildi." });
    }

    // --- API ENDPOINT: MESAJLARI GETİR ---
    [HttpGet("messages/{friendId}")]
    public async Task<IActionResult> GetMessages(string friendId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        // Kullanıcının arkadaş olup olmadığını kontrol et
        var user = await _usersCollection.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
        if (user == null || !user.FavoriteUserIds.Contains(friendId))
        {
            return BadRequest(new { message = "Bu kullanıcıyla arkadaş değilsiniz." });
        }

        // İki kullanıcı arasındaki mesajları getir
        var messages = await _chatMessagesCollection.Find(
            m => (m.SenderId == currentUserId && m.ReceiverId == friendId) ||
                 (m.SenderId == friendId && m.ReceiverId == currentUserId)
        )
        .SortBy(m => m.SentAt)
        .ToListAsync();

        // Okunmamış mesajları okundu olarak işaretle
        var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && m.ReadAt == null).ToList();
        if (unreadMessages.Any())
        {
            var unreadIds = unreadMessages.Select(m => m.Id).ToList();
            var update = Builders<ChatMessage>.Update.Set(m => m.ReadAt, DateTime.UtcNow);
            await _chatMessagesCollection.UpdateManyAsync(
                m => unreadIds.Contains(m.Id),
                update
            );
        }

        var result = messages.Select(m => new
        {
            m.Id,
            m.Message,
            m.SentAt,
            m.ReadAt,
            IsFromMe = m.SenderId == currentUserId,
            SenderName = m.SenderId == currentUserId ? "Sen" : "Arkadaş"
        });

        return Ok(result);
    }

    // --- API ENDPOINT: OKUNMAMIŞ MESAJ SAYISI ---
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadMessageCount()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized();
        }

        var count = await _chatMessagesCollection.CountDocumentsAsync(
            m => m.ReceiverId == currentUserId && m.ReadAt == null
        );

        return Ok(new { unreadCount = count });
    }
}

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
}
