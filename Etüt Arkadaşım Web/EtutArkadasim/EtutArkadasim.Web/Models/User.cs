using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    public class User
    {
        [BsonId] // MongoDB için primary key
        [BsonRepresentation(BsonType.ObjectId)] // ObjectId olarak saklanacak
        public string? Id { get; set; }

        [BsonElement("name")] // Veritabanındaki kolon adı
        public string Name { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty; // Şifreyi her zaman hash'leyerek saklayacağız

        [BsonElement("profileImageUrl")]
        public string ProfileImageUrl { get; set; } = string.Empty;

        // Kullanıcının seçtiği derslerin Id'leri
        [BsonElement("selectedCourseIds")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> SelectedCourseIds { get; set; } = new List<string>();

        // Kullanıcının favori arkadaşlarının Id'leri
        [BsonElement("favoriteUserIds")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> FavoriteUserIds { get; set; } = new List<string>();

        // Alınan puanların listesi (Basit bir ortalama için)
        [BsonElement("ratings")]
        public List<int> Ratings { get; set; } = new List<int>();

        // Ortalama puanı hesaplamak için bir yardımcı özellik
        [BsonIgnore] // Bu alanı veritabanına kaydetme
        public double AverageRating => Ratings.Any() ? Ratings.Average() : 0;
    }
}
