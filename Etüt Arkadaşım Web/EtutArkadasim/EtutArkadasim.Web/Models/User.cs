using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace EtutArkadasim.Web.Models
{
    [BsonIgnoreExtraElements]

    public class UserRating
    {
        public string RaterUserId { get; set; } // Puanı veren kişi
        public int Score { get; set; }          // Verdiği puan
    }
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // --- DİKKAT: Veritabanındaki alan adlarıyla birebir aynı olmalı ---

        [BsonElement("name")] // Eskiden "name" idi, şimdi "Name" yaptık
        public string Name { get; set; } = string.Empty;

        [BsonElement("Email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("ProfileImageUrl")]
        public string ProfileImageUrl { get; set; } = string.Empty;

        [BsonElement("DepartmentId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? DepartmentId { get; set; }

        // --- KONUM BİLGİLERİ ---
        [BsonElement("City")]
        public string? City { get; set; }

        [BsonElement("District")]
        public string? District { get; set; }

        [BsonElement("PreferredLocationsText")]
        public string? PreferredLocationsText { get; set; }
        // -----------------------

        [BsonElement("selectedCourseIds")] // Bunlar genelde küçük harfle başlar, dokunmadık
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> SelectedCourseIds { get; set; } = new List<string>();

        [BsonElement("favoriteUserIds")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> FavoriteUserIds { get; set; } = new List<string>();

        [BsonElement("ratings")]
        public List<UserRating> Ratings { get; set; } = new List<UserRating>();

        [BsonIgnore] // Ortalamayı artık UserRating listesinden hesaplıyoruz
        public double AverageRating => Ratings.Any() ? Ratings.Average(r => r.Score) : 0;
    }
}