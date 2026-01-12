using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    public enum WeekDay
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday
    }

    public class TimeSlot
    {
        [BsonElement("startTime")]
        public string StartTime { get; set; } = "09:00"; // HH:mm format

        [BsonElement("endTime")]
        public string EndTime { get; set; } = "17:00"; // HH:mm format

        [BsonElement("isAvailable")]
        public bool IsAvailable { get; set; } = true;
    }

    public class DailySchedule
    {
        [BsonElement("day")]
        [BsonRepresentation(BsonType.String)]
        public WeekDay Day { get; set; }

        [BsonElement("timeSlots")]
        public List<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();

        [BsonElement("isAvailable")]
        public bool IsAvailable { get; set; } = true; // Bu gün tamamen müsait mi
    }

    public class StudySchedule
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("weeklySchedule")]
        public List<DailySchedule> WeeklySchedule { get; set; } = new List<DailySchedule>();

        [BsonElement("timezone")]
        public string Timezone { get; set; } = "Europe/Istanbul"; // Zaman dilimi

        [BsonElement("isFlexible")]
        public bool IsFlexible { get; set; } = true; // Esnek zamanlama mı

        [BsonElement("preferredAdvanceNotice")]
        public int PreferredAdvanceNotice { get; set; } = 24; // Saat olarak önceden haber verme

        [BsonElement("maxSessionsPerDay")]
        public int MaxSessionsPerDay { get; set; } = 3; // Günde maksimum çalışma sayısı

        [BsonElement("createdAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
