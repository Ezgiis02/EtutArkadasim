using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    public enum LocationType { Library, Faculty, Campus, Cafe, StudyRoom, Online, Other }
    public enum NoiseLevel { Silent, Quiet, Moderate, Busy }

    public class Location
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("LocationType")]
        [BsonRepresentation(BsonType.String)]
        public LocationType LocationType { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("City")]
        public string City { get; set; } = string.Empty;

        [BsonElement("District")]
        public string District { get; set; } = string.Empty;

        [BsonElement("Address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("NoiseLevel")]
        [BsonRepresentation(BsonType.String)]
        public NoiseLevel NoiseLevel { get; set; } = NoiseLevel.Quiet;

        // --- KIRMIZI ÇİZGİLERİ GİDERECEK OLAN YENİ ALANLAR ---

        [BsonElement("HasWifi")]
        public bool HasWifi { get; set; } = true;

        [BsonElement("HasPowerOutlets")]
        public bool HasPowerOutlets { get; set; } = true;

        [BsonElement("AllowsDiscussion")]
        public bool AllowsDiscussion { get; set; } = true;

        [BsonElement("OpeningHours")]
        public string OpeningHours { get; set; } = "08:00-22:00";

        [BsonElement("Capacity")]
        public int Capacity { get; set; } = 50;

        [BsonElement("IsActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("CreatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

