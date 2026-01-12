using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EtutArkadasim.Web.Models
{
    public class ChatMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Mesajı gönderen kullanıcının Id'si
        [BsonElement("senderId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SenderId { get; set; } = string.Empty;

        // Mesajı alan kullanıcının Id'si
        [BsonElement("receiverId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ReceiverId { get; set; } = string.Empty;

        // Mesaj içeriği
        [BsonElement("message")]
        public string Message { get; set; } = string.Empty;

        // Mesajın gönderildiği tarih
        [BsonElement("sentAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Mesajın okunduğu tarih (null ise okunmamış)
        [BsonElement("readAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? ReadAt { get; set; }
    }
}
