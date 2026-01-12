using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using System.Collections.Generic;

namespace EtutArkadasim.Web.Models
{
    public enum CourseLevel { Undergraduate, Graduate, Doctorate }
    public enum CourseLanguage { Turkish, English }

    public class Course
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // MongoDB'deki "CourseName" ile buradaki "CourseName"i eşleştiriyoruz
        [BsonElement("CourseName")]
        public string CourseName { get; set; } = string.Empty;

        [BsonElement("CourseCode")]
        public string CourseCode { get; set; } = string.Empty;

        [BsonElement("Department")]
        public string Department { get; set; } = string.Empty;

        [BsonElement("Description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("Credits")]
        public int Credits { get; set; } = 3;

        [BsonElement("Semester")]
        public int Semester { get; set; } = 1;

        // Veritabanında olmayan alanlar için varsayılan değerler atanmaya devam eder
        [BsonElement("Level")]
        [BsonRepresentation(BsonType.String)]
        public CourseLevel Level { get; set; } = CourseLevel.Undergraduate;

        [BsonElement("Language")]
        [BsonRepresentation(BsonType.String)]
        public CourseLanguage Language { get; set; } = CourseLanguage.Turkish;

        [BsonElement("Prerequisites")]
        public List<string> Prerequisites { get; set; } = new List<string>();

        [BsonElement("IsActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("CreatedAt")]
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}