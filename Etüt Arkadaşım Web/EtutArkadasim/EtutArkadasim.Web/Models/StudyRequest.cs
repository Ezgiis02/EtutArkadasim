using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EtutArkadasim.Web.Models
{
    // Talebin durumunu belirtmek için sabitler (string de kullanabilirdik)
    public enum RequestStatus
    {
        Pending,  // Beklemede
        Accepted, // Kabul Edildi
        Rejected  // Reddedildi
    }

    public class StudyRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Talebi gönderen kullanıcının Id'si
        [BsonElement("senderId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string SenderId { get; set; } = string.Empty;

        // Talebi alan kullanıcının Id'si
        [BsonElement("receiverId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ReceiverId { get; set; } = string.Empty;

        // Talebin durumu (Pending, Accepted, Rejected)
        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)] // Enum'ı string olarak sakla
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [BsonElement("requestedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        // Talebin hangi ders için yapıldığı
        [BsonElement("courseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CourseId { get; set; } = string.Empty;
    }
}