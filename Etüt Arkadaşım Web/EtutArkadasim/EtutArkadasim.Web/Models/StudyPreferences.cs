using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    public enum StudyTimePreference
    {
        Morning,    // Sabah (06:00-12:00)
        Afternoon,  // Öğle (12:00-18:00)
        Evening,    // Akşam (18:00-22:00)
        Night,      // Gece (22:00-06:00)
        Flexible    // Esnek
    }

    public enum StudyEnvironmentPreference
    {
        Quiet,      // Sessiz ortam
        Discussion, // Tartışarak
        Music,      // Müzikli
        Flexible    // Esnek
    }

    public enum StudyFormatPreference
    {
        Online,     // Online
        InPerson,   // Yüz yüze
        Hybrid,     // Hibrit
        Flexible    // Esnek
    }

    public enum MotivationStyle
    {
        Competitive,    // Rekabetçi
        Collaborative,  // İş birlikçi
        Independent,    // Bağımsız
        Mentored        // Mentorluk
    }

    public class StudyPreferences
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        // Çalışma zamanı tercihleri
        [BsonElement("preferredStudyTimes")]
        [BsonRepresentation(BsonType.String)]
        public List<StudyTimePreference> PreferredStudyTimes { get; set; } = new List<StudyTimePreference>();

        // Çalışma ortamı tercihleri
        [BsonElement("preferredEnvironments")]
        [BsonRepresentation(BsonType.String)]
        public List<StudyEnvironmentPreference> PreferredEnvironments { get; set; } = new List<StudyEnvironmentPreference>();

        // Çalışma formatı tercihleri
        [BsonElement("preferredFormats")]
        [BsonRepresentation(BsonType.String)]
        public List<StudyFormatPreference> PreferredFormats { get; set; } = new List<StudyFormatPreference>();

        // Motivasyon tarzı
        [BsonElement("motivationStyle")]
        [BsonRepresentation(BsonType.String)]
        public MotivationStyle MotivationStyle { get; set; } = MotivationStyle.Collaborative;

        // Tercih edilen çalışma süresi (dakika)
        [BsonElement("preferredSessionDuration")]
        public int PreferredSessionDuration { get; set; } = 120; // 2 saat varsayılan

        // Ara verme sıklığı (dakika)
        [BsonElement("breakFrequency")]
        public int BreakFrequency { get; set; } = 45; // 45 dakika çalış, 15 dakika ara

        // Haftalık çalışma hedefi (saat)
        [BsonElement("weeklyStudyGoal")]
        public int WeeklyStudyGoal { get; set; } = 20; // 20 saat varsayılan

        // Özel notlar/tercihler
        [BsonElement("specialNotes")]
        public string SpecialNotes { get; set; } = string.Empty;

        // Konum tercihleri (şehir, ilçe bazlı)
        [BsonElement("preferredCities")]
        public List<string> PreferredCities { get; set; } = new List<string>();

        [BsonElement("preferredDistricts")]
        public List<string> PreferredDistricts { get; set; } = new List<string>();

        // Tercih edilen yerler
        [BsonElement("preferredLocations")]
        public List<string> PreferredLocations { get; set; } = new List<string>(); // Kütüphane, fakülte, kampüs vs.

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
