using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    public enum SessionType
    {
        Study,     // Çalışma
        Review,    // Gözden geçirme
        ExamPrep,  // Sınav hazırlık
        GroupWork  // Grup çalışması
    }

    public enum SessionStatus
    {
        Scheduled, // Planlandı
        Ongoing,   // Devam ediyor
        Completed, // Tamamlandı
        Cancelled  // İptal edildi
    }

    public class StudySession
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("sessionName")]
        public string SessionName { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("courseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CourseId { get; set; } = string.Empty; // Hangi ders için

        [BsonElement("organizerId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string OrganizerId { get; set; } = string.Empty; // Düzenleyen kişi

        [BsonElement("participantIds")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> ParticipantIds { get; set; } = new List<string>(); // Katılımcı ID'leri

        [BsonElement("maxParticipants")]
        public int MaxParticipants { get; set; } = 5;

        [BsonElement("sessionType")]
        [BsonRepresentation(BsonType.String)]
        public SessionType SessionType { get; set; } = SessionType.Study;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

        [BsonElement("scheduledDate")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime ScheduledDate { get; set; } // Planlanan tarih

        [BsonElement("durationHours")]
        public double DurationHours { get; set; } = 2.0; // Süre (saat)

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty; // Yer/online

        [BsonElement("meetingLink")]
        public string MeetingLink { get; set; } = string.Empty; // Online toplantı linki

        [BsonElement("topics")]
        public List<string> Topics { get; set; } = new List<string>(); // Konular

        [BsonElement("isPublic")]
        public bool IsPublic { get; set; } = true; // Herkes katılabilir mi

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
