using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EtutArkadasim.Web.Models
{
    public enum AnnouncementType
    {
        System,      // Sistem duyurusu
        Course,      // Ders duyurusu
        Event,       // Etkinlik duyurusu
        Maintenance  // Bakım duyurusu
    }

    public enum AnnouncementPriority
    {
        Low,     // Düşük
        Normal,  // Normal
        High,    // Yüksek
        Urgent   // Acil
    }

    public class Announcement
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("announcementType")]
        [BsonRepresentation(BsonType.String)]
        public AnnouncementType AnnouncementType { get; set; } = AnnouncementType.System;

        [BsonElement("priority")]
        [BsonRepresentation(BsonType.String)]
        public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Normal;

        [BsonElement("courseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CourseId { get; set; } // Ders duyurusuysa hangi ders

        [BsonElement("authorId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string AuthorId { get; set; } = string.Empty; // Duyuruyu yayınlayan kişi

        [BsonElement("targetAudience")]
        public string TargetAudience { get; set; } = "all"; // Hedef kitle (all, students, instructors)

        [BsonElement("isPublished")]
        public bool IsPublished { get; set; } = true;

        [BsonElement("publishDate")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;

        [BsonElement("expiryDate")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime? ExpiryDate { get; set; } // Son geçerlilik tarihi

        [BsonElement("viewCount")]
        public int ViewCount { get; set; } = 0;

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
