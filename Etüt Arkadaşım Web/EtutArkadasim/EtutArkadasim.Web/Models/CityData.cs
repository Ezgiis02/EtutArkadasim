using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    [BsonIgnoreExtraElements] // Hata önleyici sigorta
    public class CityData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // DİKKAT: JSON dosyasında "CityName" yazıyor, burası birebir aynı olmalı
        [BsonElement("CityName")]
        public string CityName { get; set; } = null!;

        [BsonElement("Districts")]
        public List<string> Districts { get; set; } = new List<string>();
    }
}